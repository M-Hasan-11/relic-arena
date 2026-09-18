using UnityEngine;
using UnityEngine.InputSystem;
namespace RelicArena
{
    public class Guardian : MonoBehaviour
    {
        public float health=100,maxHealth=100,dashCooldown,pulseCooldown;
        public float swordDamage=36,pulseRecovery=7;
        public Animator animator;
        public ArenaMotor motor;
        RelicGame game;RigidPose pose;
        string animationState="";
        float attackCooldown,attackTime,invulnerable,dashTime,rollVisual,pulseVisual,motion,strikeDelay;
        int combo;bool strikePending;
        Vector3 dashDirection,smoothedInput;
        public void Setup(RelicGame owner,Transform model)
        {
            game=owner;pose=new RigidPose(model);animator=model.GetComponentInChildren<Animator>();
            if(animator)animator.applyRootMotion=false;
            gameObject.layer=9;motor=gameObject.AddComponent<ArenaMotor>();motor.Initialize();
        }
        public void ResetGuardian()
        {
            health=maxHealth=100;swordDamage=36;pulseRecovery=7;animationState="";
            dashCooldown=pulseCooldown=attackCooldown=attackTime=invulnerable=dashTime=rollVisual=pulseVisual=0;
            smoothedInput=Vector3.zero;strikePending=false;combo=0;
            motor.Warp(new Vector3(0,.1f,-5));transform.rotation=Quaternion.identity;
        }
        void Update()
        {
            if(game.State!=SessionState.Playing){motion=0;Animate(health<=0?"Death":"Idle");return;}
            float dt=Time.deltaTime;
            dashCooldown=Mathf.Max(0,dashCooldown-dt);pulseCooldown=Mathf.Max(0,pulseCooldown-dt);
            attackCooldown=Mathf.Max(0,attackCooldown-dt);attackTime=Mathf.Max(0,attackTime-dt);
            invulnerable=Mathf.Max(0,invulnerable-dt);rollVisual=Mathf.Max(0,rollVisual-dt);pulseVisual=Mathf.Max(0,pulseVisual-dt);
            if(strikePending){strikeDelay-=dt;if(strikeDelay<=0){strikePending=false;StrikeImpact();}}
            Vector2 input=Vector2.zero;var k=Keyboard.current;
            if(k!=null){input.x=(k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0);input.y=(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0);}
            var direction=game.CameraDirection(input.normalized);
            smoothedInput=Vector3.MoveTowards(smoothedInput,direction,dt*12);
            if(k!=null && k.spaceKey.wasPressedThisFrame)Dodge(direction);
            if(k!=null && k.qKey.wasPressedThisFrame)Pulse();
            if((Mouse.current!=null&&Mouse.current.leftButton.isPressed)||(k!=null&&k.jKey.isPressed))Strike();
            if(dashTime>0){dashTime-=dt;Move(dashDirection,dt,20);}
            else Move(smoothedInput,dt,attackTime>0||pulseVisual>0?2.2f:6.5f);
            motion=new Vector2(motor.Velocity.x,motor.Velocity.z).magnitude;
            Animate(rollVisual>0?"Dodge":pulseVisual>0?"Pulse":attackTime>0?(combo%2==0?"Attack2":"Attack"):motion>.2f?"Run":"Idle");
        }
        void Animate(string state)
        {
            if(animator && animator.runtimeAnimatorController)
            {
                animator.SetFloat("Speed",motion,.10f,Time.deltaTime);
                if(state=="Idle"||state=="Run")state="Locomotion";
                if(animationState!=state){animator.CrossFadeInFixedTime(state,state=="Dodge"?.055f:.11f);animationState=state;}
            }
            else pose.Animate(motion/6.5f,attackTime>0?1-attackTime/.58f:0,Time.time);
        }
        public void Move(Vector3 direction,float dt,float speed=6.5f)
        {
            motor.Move(direction*speed*dt,dt);
            if(direction.sqrMagnitude>.01f && attackTime<=0 && pulseVisual<=0)
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-17*dt));
        }
        public void Strike()
        {
            if(attackCooldown>0||rollVisual>0||pulseVisual>0||game.State!=SessionState.Playing)return;
            attackCooldown=.6f;attackTime=.58f;strikeDelay=.19f;strikePending=true;combo++;
            Sentinel closest=null;float distance=3.2f;
            foreach(var e in game.enemies)
            {if(!e)continue;float d=Vector3.Distance(transform.position,e.transform.position);if(d<distance){closest=e;distance=d;}}
            if(closest){var d=closest.transform.position-transform.position;d.y=0;if(d.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(d);}
        }
        void StrikeImpact()
        {
            game.Sound("slash");
            var arc=new GameObject("Sword slash");arc.transform.position=transform.position+Vector3.up;
            arc.transform.rotation=transform.rotation;
            var line=arc.AddComponent<LineRenderer>();line.sharedMaterial=game.tealFx;line.useWorldSpace=false;line.widthMultiplier=.09f;line.positionCount=21;
            for(int i=0;i<21;i++){float a=Mathf.Lerp(-75,75,i/20f)*Mathf.Deg2Rad;line.SetPosition(i,new Vector3(Mathf.Sin(a)*2.3f,Mathf.Sin(a)*.3f,Mathf.Cos(a)*2.3f));}
            arc.AddComponent<RelicEffect>().life=.18f;
            foreach(var e in game.enemies.ToArray())
            {
                if(!e)continue;var delta=e.transform.position-transform.position;delta.y=0;
                if(delta.magnitude<(e.isBoss?3.7f:3.0f) && Vector3.Dot(transform.forward,delta.normalized)>.12f &&
                   !Physics.Linecast(transform.position+Vector3.up,e.transform.position+Vector3.up,1<<8))e.Hurt(swordDamage);
            }
        }
        public void Dodge(Vector3 direction)
        {
            if(dashCooldown>0||game.State!=SessionState.Playing)return;
            dashCooldown=1.25f;dashTime=.24f;rollVisual=.52f;invulnerable=.34f;
            strikePending=false;attackTime=pulseVisual=0;
            dashDirection=direction.sqrMagnitude>.1f?direction.normalized:transform.forward;
            transform.rotation=Quaternion.LookRotation(dashDirection);
            game.Ring(transform.position+Vector3.up*.1f,1,game.tealFx,.3f);game.Sound("dash");
            game.Burst(transform.position+Vector3.up*.2f,new Color(.3f,.8f,1),12,2);
        }
        public void Pulse()
        {
            if(pulseCooldown>0||rollVisual>0||game.State!=SessionState.Playing)return;
            pulseCooldown=pulseRecovery;pulseVisual=.65f;attackTime=0;strikePending=false;
            game.Ring(transform.position+Vector3.up*.15f,6,game.tealFx,.55f);
            game.Ring(transform.position+Vector3.up*.25f,5.5f,game.goldFx,.5f);
            game.Sound("pulse");game.Shake(.13f);game.Burst(transform.position+Vector3.up*.3f,new Color(.15f,1,.82f),55,7);
            foreach(var e in game.enemies.ToArray())if(e&&Vector3.Distance(transform.position,e.transform.position)<6)e.Hurt(60);
        }
        public void Hurt(float damage)
        {
            if(invulnerable>0||game.State!=SessionState.Playing)return;
            health=Mathf.Max(0,health-damage);invulnerable=.6f;
            game.Sound("hurt");game.Shake(.17f);game.Sparks(transform.position+Vector3.up,game.redFx,8);
            if(health<=0){strikePending=false;game.End(false);}
        }
    }

    // Original Blender parts use explicit pivots, so lightweight rigid animation stays editable.
    public class RigidPose
    {
        Transform leftLeg,rightLeg,leftArm,rightArm,torso;
        Quaternion ll,rl,la,ra,tt;
        public RigidPose(Transform root)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>())
            {
                // Blender gives the second exported character numeric suffixes.
                string n=t.name.Split('.')[0];
                if(n=="LegL")leftLeg=t;if(n=="LegR")rightLeg=t;
                if(n=="ArmL")leftArm=t;if(n=="ArmR")rightArm=t;if(n=="Torso")torso=t;
            }
            if(leftLeg)ll=leftLeg.localRotation;if(rightLeg)rl=rightLeg.localRotation;
            if(leftArm)la=leftArm.localRotation;if(rightArm)ra=rightArm.localRotation;if(torso)tt=torso.localRotation;
        }
        public void Animate(float run,float strike,float time)
        {
            float swing=Mathf.Sin(time*12)*30*run;
            if(leftLeg)leftLeg.localRotation=ll*Quaternion.Euler(swing,0,0);
            if(rightLeg)rightLeg.localRotation=rl*Quaternion.Euler(-swing,0,0);
            if(leftArm)leftArm.localRotation=la*Quaternion.Euler(-swing*.65f,0,0);
            if(rightArm)rightArm.localRotation=ra*Quaternion.Euler(strike>0?-50+Mathf.Sin(strike*Mathf.PI)*100:swing*.65f,strike>0?Mathf.Lerp(-65,85,strike):0,strike>0?-35:0);
            if(torso)torso.localRotation=tt*Quaternion.Euler(0,0,Mathf.Sin(time*2)*1.5f);
        }
    }
}
