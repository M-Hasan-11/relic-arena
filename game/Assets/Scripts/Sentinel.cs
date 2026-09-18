using UnityEngine;
namespace RelicArena
{
    public class Sentinel : MonoBehaviour
    {
        public float health,maxHealth;
        public bool elite,ranged,isBoss;
        public bool IsWinding => windup>0;
        public bool Enraged => isBoss && health<maxHealth*.5f;
        public string BossName => new[]{"THE GATE WARDEN","CINDER TYRANT","THE ASTRAL KING"}[game.Level];
        public ArenaMotor motor;
        RelicGame game;RigidPose pose;Animator animator;
        float windup,recovery=.9f,stun,strikeVisual,phase,charge;
        int pattern;
        Vector3 attackDirection,lockedTarget;
        GameObject tell;string state;
        public void Setup(RelicGame owner,Transform model,bool champion)
        {
            game=owner;elite=champion;maxHealth=health=(elite?160:85)*(1+game.Level*.2f);
            pose=new RigidPose(model);animator=model.GetComponentInChildren<Animator>();
            if(animator)animator.applyRootMotion=false;
            gameObject.layer=10;motor=gameObject.AddComponent<ArenaMotor>();motor.Initialize(.38f,1.85f);
            phase=(game.enemies.Count%2==0?1:-1);recovery=.7f+(game.enemies.Count%3)*.22f;
        }
        public void MakeBoss()
        {
            isBoss=elite=true;maxHealth=health=550+game.Level*200;
            motor.controller.radius=.8f;motor.controller.height=3.3f;motor.controller.center=Vector3.up*1.68f;
            game.boss=this;
            var accent=game.Level==0?game.goldFx:game.Level==1?game.redFx:game.tealFx;
            foreach(var renderer in GetComponentsInChildren<Renderer>())
            {
                var mats=renderer.sharedMaterials;
                for(int i=0;i<mats.Length;i++)if(mats[i] && mats[i].name.Contains("Glow"))mats[i]=accent;
                renderer.sharedMaterials=mats;
            }
            for(int i=0;i<3+game.Level*2;i++)
            {
                var crystal=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(crystal.GetComponent<Collider>());
                crystal.name="Boss crown crystal";crystal.transform.parent=transform;
                float a=i*Mathf.PI*2/(3+game.Level*2);
                crystal.transform.localPosition=new Vector3(Mathf.Sin(a)*.7f,3.75f,Mathf.Cos(a)*.7f);
                crystal.transform.localScale=new Vector3(.2f,.65f,.2f);crystal.transform.localRotation=Quaternion.Euler(15,a*Mathf.Rad2Deg,20);
                crystal.GetComponent<Renderer>().sharedMaterial=accent;
            }
        }
        void Animate(string next,float run=0)
        {
            if(animator){if(state!=next){animator.CrossFadeInFixedTime(next,.12f);state=next;}}
            else pose.Animate(run,strikeVisual>0?1-strikeVisual/.45f:0,Time.time);
        }
        void Update()
        {
            if(game.State!=SessionState.Playing)return;
            float dt=Time.deltaTime;strikeVisual=Mathf.Max(0,strikeVisual-dt);
            motor.Move(Vector3.zero,dt);
            if(stun>0){stun-=dt;Animate("Hit");return;}
            Vector3 delta=game.hero.transform.position-transform.position;delta.y=0;float distance=delta.magnitude;
            if(charge>0)
            {
                charge-=dt;motor.Move(attackDirection*12*dt,dt);Animate("Run",1);
                if(distance<1.6f)game.hero.Hurt(Enraged?32:26);
                if(charge<=0){game.Burst(transform.position+Vector3.up,Color.red,35,5);recovery=1.25f;}
                return;
            }
            if(windup>0)
            {
                windup-=dt;Animate("Windup");
                if(windup<=0){if(tell)Destroy(tell);ResolveAttack(delta,distance);}
                return;
            }
            recovery-=dt;
            if(strikeVisual>0){Animate(ranged?"Pulse":"Attack");return;}
            if(isBoss && recovery<=0){PrepareBoss(delta);return;}
            if(ranged){Ranged(delta,distance,dt);return;}
            if(distance<(isBoss?3.6f:2.25f) && recovery<=0 && game.CanAttack(this))
            {
                attackDirection=distance>.01f?delta.normalized:transform.forward;
                transform.rotation=Quaternion.LookRotation(attackDirection);
                windup=elite?.65f:.78f;pattern=0;
                Warning(transform.position+attackDirection*1.1f,1.5f);return;
            }
            if(distance>1.8f)
            {
                // Close on an offset flank; soften group separation instead of stacking.
                var direction=delta.normalized;
                if(distance>3 && distance<9)direction+=Vector3.Cross(Vector3.up,direction)*phase*.42f;
                foreach(var e in game.enemies)
                {
                    if(!e||e==this)continue;var apart=transform.position-e.transform.position;apart.y=0;
                    if(apart.sqrMagnitude<2.5f && apart.sqrMagnitude>.001f)direction+=apart.normalized*(1.6f-apart.magnitude);
                }
                Move(direction,(isBoss?2.4f:elite?3.6f:3.05f)+game.Level*.25f,dt);
            }
            else Animate("Idle");
        }
        void Move(Vector3 direction,float speed,float dt)
        {
            direction=motor.Steer(direction);motor.Move(direction*speed*dt,dt);
            if(direction.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-9*dt));
            Animate("Run",1);
        }
        void Ranged(Vector3 delta,float distance,float dt)
        {
            Vector3 direction=delta.normalized*(distance<7?-1:distance>11?1:0);
            if(distance>=7 && distance<=11)direction=Vector3.Cross(Vector3.up,delta.normalized)*phase*.55f;
            Move(direction,2.9f,dt);
            if(recovery<=0 && distance<18 && game.CanAttack(this) && !Physics.Linecast(transform.position+Vector3.up,game.hero.transform.position+Vector3.up,1<<8))
            {
                // Predict briefly, then lock the aim so the warning remains dodgeable.
                attackDirection=(delta+game.hero.motor.Velocity*.18f).normalized;attackDirection.y=0;attackDirection.Normalize();
                windup=.85f;pattern=3;LineWarning(attackDirection,18);transform.rotation=Quaternion.LookRotation(attackDirection);
            }
        }
        void PrepareBoss(Vector3 delta)
        {
            pattern=1+(pattern%3); // slam, charge, projectile fan
            if(game.Level==0 && pattern==3)pattern=1;
            attackDirection=delta.sqrMagnitude>.01f?delta.normalized:transform.forward;
            lockedTarget=transform.position;
            transform.rotation=Quaternion.LookRotation(attackDirection);
            windup=pattern==1?1.15f:pattern==2?1.0f:.95f;
            if(pattern==1)Warning(lockedTarget,Enraged?6:5);
            else LineWarning(attackDirection,pattern==2?13:18);
        }
        void ResolveAttack(Vector3 delta,float distance)
        {
            strikeVisual=.48f;state=null;
            if(isBoss)
            {
                if(pattern==1)
                {
                    float radius=Enraged?6:5;
                    game.Ring(lockedTarget+Vector3.up*.1f,radius,game.redFx,.5f);game.Shake(.23f);game.Sound("pulse");
                    game.Burst(lockedTarget+Vector3.up*.2f,new Color(1,.35f,.1f),70,9);
                    if(Vector3.Distance(game.hero.transform.position,lockedTarget)<radius)game.hero.Hurt(Enraged?34:26);
                }
                else if(pattern==2){charge=.8f;strikeVisual=0;}
                else {for(int i=-2;i<=2;i++)Bolt(Quaternion.Euler(0,i*13,0)*attackDirection,9+game.Level);}
                recovery=Enraged?1.05f:1.65f;
            }
            else if(ranged){Bolt(attackDirection,9+game.Level);recovery=1.6f;}
            else
            {
                if(distance<2.85f && Vector3.Dot(attackDirection,delta.normalized)>.3f)game.hero.Hurt(elite?23:16+game.Level*2);
                game.Ring(transform.position+attackDirection+Vector3.up*.1f,1.2f,game.redFx,.2f);recovery=elite?.85f:1.05f;
            }
        }
        void Bolt(Vector3 direction,float speed)
        {
            var bolt=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(bolt.GetComponent<Collider>());
            bolt.name="Enemy firebolt";bolt.transform.position=transform.position+Vector3.up+direction*(isBoss?1.2f:.65f);bolt.transform.localScale=Vector3.one*.28f;
            bolt.GetComponent<Renderer>().sharedMaterial=game.redFx;
            var trail=bolt.AddComponent<TrailRenderer>();trail.sharedMaterial=game.redFx;trail.time=.23f;trail.startWidth=.22f;trail.endWidth=0;
            var shot=bolt.AddComponent<RelicBolt>();shot.game=game;shot.velocity=direction*speed;shot.damage=isBoss?20:13+game.Level;
        }
        void Warning(Vector3 position,float radius)
        {
            tell=new GameObject("Danger / dodge outside ring");tell.transform.position=position+Vector3.up*.08f;
            var line=tell.AddComponent<LineRenderer>();line.sharedMaterial=game.redFx;line.loop=true;line.widthMultiplier=.1f;line.positionCount=64;line.useWorldSpace=false;
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;line.SetPosition(i,new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius));}
        }
        void LineWarning(Vector3 direction,float length)
        {
            tell=new GameObject("Danger / locked attack direction");var line=tell.AddComponent<LineRenderer>();line.sharedMaterial=game.redFx;
            line.widthMultiplier=isBoss?.65f:.055f;line.positionCount=2;
            line.SetPosition(0,transform.position+Vector3.up*.08f);line.SetPosition(1,transform.position+direction*length+Vector3.up*.08f);
        }
        public void Hurt(float amount)
        {
            if(health<=0)return;
            health=Mathf.Max(0,health-amount);
            if(!isBoss && (!elite || !IsWinding)){stun=.16f;windup=0;recovery=.48f;if(tell)Destroy(tell);}
            game.DamageNumber(transform.position,amount);game.Sound("hit");game.Shake(.035f);game.Sparks(transform.position+Vector3.up,game.goldFx,6);
            var away=transform.position-game.hero.transform.position;away.y=0;
            if(!isBoss)motor.Move(away.normalized*.18f,.05f,false);
            if(health<=0)
            {
                if(tell)Destroy(tell);game.Ring(transform.position+Vector3.up*.1f,isBoss?4:1.3f,game.redFx,.5f);
                game.EnemyFell(this);motor.controller.enabled=false;enabled=false;Animate("Death");
                if(isBoss)game.Burst(transform.position+Vector3.up*2,new Color(1,.45f,.1f),100,9);
                Destroy(gameObject,animator?1.3f:.05f);
            }
        }
        void OnDestroy(){if(tell)Destroy(tell);}
    }
}
