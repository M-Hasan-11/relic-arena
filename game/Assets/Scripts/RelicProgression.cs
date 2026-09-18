using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RelicArena
{
    public partial class RelicGame
    {
        public Material particleMaterial, mistMaterial;
        public int bestScore;
        public float sensitivity=.13f, masterVolume=.8f, cameraDistance=8.5f;
        bool upgradeChosen;
        class DamageText { public Vector3 position; public string text; public float end; }
        readonly List<DamageText> damageTexts=new List<DamageText>();

        void LoadPreferences()
        {
            bestScore=PlayerPrefs.GetInt("Relic.Best",0);
            sensitivity=PlayerPrefs.GetFloat("Relic.Sensitivity",.13f);
            masterVolume=PlayerPrefs.GetFloat("Relic.Volume",.8f);
            audioSource.volume=masterVolume;
        }
        void RecordBest()
        {
            if(smoke)return;
            if(score>bestScore){bestScore=score;PlayerPrefs.SetInt("Relic.Best",bestScore);PlayerPrefs.Save();}
        }
        void UpdatePreferencesInput()
        {
            if(Keyboard.current!=null && Keyboard.current.mKey.wasPressedThisFrame)
            {masterVolume=masterVolume>.01f?0:.8f;SavePreferences();}
            if(State==SessionState.Choosing && Keyboard.current!=null)
            {
                if(Keyboard.current.digit1Key.wasPressedThisFrame)ChooseUpgrade(0);
                else if(Keyboard.current.digit2Key.wasPressedThisFrame)ChooseUpgrade(1);
                else if(Keyboard.current.digit3Key.wasPressedThisFrame)ChooseUpgrade(2);
            }
        }
        void SavePreferences()
        {
            audioSource.volume=masterVolume;
            PlayerPrefs.SetFloat("Relic.Volume",masterVolume);PlayerPrefs.SetFloat("Relic.Sensitivity",sensitivity);PlayerPrefs.Save();
        }
        public void OpenUpgrade()
        {State=SessionState.Choosing;Time.timeScale=0;LockCursor(false);}
        public void ChooseUpgrade(int choice)
        {
            if(State!=SessionState.Choosing || choice<0 || choice>2)return;
            if(choice==0)hero.swordDamage+=12;
            if(choice==1){hero.maxHealth+=25;hero.health=Mathf.Min(hero.maxHealth,hero.health+45);}
            if(choice==2)hero.pulseRecovery=Mathf.Max(3,hero.pulseRecovery-1);
            upgradeChosen=true;waveDelay=.7f;State=SessionState.Playing;Time.timeScale=1;LockCursor(true);Sound("collect");
        }
        void UpgradeGUI(float width)
        {
            Panel(new Rect(0,0,width,900),new Color(.018f,.034f,.047f,.92f));
            var center=new GUIStyle(heading){alignment=TextAnchor.MiddleCenter};
            Label(width/2-400,190,800,45,"THE TEMPLE GRANTS A RELIC",center);
            var sub=new GUIStyle(body){alignment=TextAnchor.MiddleCenter};
            Label(width/2-400,247,800,45,"Encounter cleared. Choose a blessing for the journey ahead.",sub);
            string[] names={"EDGE OF DAWN","UNBROKEN HEART","STORM CALLER"};
            string[] descriptions={"+12 sword damage\nCut down sentinels faster.","+25 maximum vitality\nRestore 45 health now.","Shockwave recharges 1s faster\nControl the space around you."};
            for(int i=0;i<3;i++)
            {
                float x=width/2-495+i*340;
                Panel(new Rect(x,355,310,285),new Color(.055f,.09f,.11f,1));Panel(new Rect(x,355,310,3),i==1?gold:mint);
                Label(x+25,378,260,45,"0"+(i+1),number);
                Label(x+25,440,275,40,names[i],body);
                Label(x+25,489,265,75,descriptions[i],body);
                if(Button(new Rect(x+25,576,260,43),"CLAIM RELIC  /  "+(i+1)))ChooseUpgrade(i);
            }
        }
        void PreferencesGUI(float width)
        {
            float x=width/2-260;
            Panel(new Rect(x,690,520,130),ink);
            Label(x+22,701,190,25,"AUDIO / M TO MUTE",tiny);
            float volume=GUI.HorizontalSlider(new Rect(x+225,712,260,18),masterVolume,0,1);
            Label(x+22,746,190,25,"MOUSE SENSITIVITY",tiny);
            float look=GUI.HorizontalSlider(new Rect(x+225,757,260,18),sensitivity,.04f,.3f);
            Label(x+22,783,475,25,"MOUSE WHEEL / ADJUST CAMERA DISTANCE",tiny);
            if(Mathf.Abs(volume-masterVolume)>.001f||Mathf.Abs(look-sensitivity)>.001f)
            {masterVolume=volume;sensitivity=look;SavePreferences();}
        }
        public void DamageNumber(Vector3 p,float damage)
        {damageTexts.Add(new DamageText{position=p+Vector3.up*2,text=Mathf.CeilToInt(damage).ToString(),end=Time.time+.7f});}
        void DrawDamageNumbers(float scale)
        {
            if(State!=SessionState.Playing)return;
            for(int i=damageTexts.Count-1;i>=0;i--)
            {
                var d=damageTexts[i];float remaining=d.end-Time.time;
                if(remaining<=0){damageTexts.RemoveAt(i);continue;}
                var p=view.WorldToScreenPoint(d.position+Vector3.up*(.7f-remaining));
                if(p.z<=0)continue;
                GUI.color=new Color(1,.85f,.48f,Mathf.Min(1,remaining*3));
                Label(p.x/scale-25,(Screen.height-p.y)/scale,80,45,d.text,heading);GUI.color=Color.white;
            }
        }
        public void Burst(Vector3 position,Color color,int count,float speed)
        {
            if(!particleMaterial)return;
            var o=new GameObject("Asset Store spark burst");o.transform.position=position;
            var ps=o.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.duration=.65f;main.startLifetime=new ParticleSystem.MinMaxCurve(.25f,.7f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.3f,speed);main.startSize=new ParticleSystem.MinMaxCurve(.06f,.18f);main.startColor=color;
            main.gravityModifier=.4f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=120;
            var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.15f;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=particleMaterial;
            ps.Play();Destroy(o,1.5f);
        }
    }

    public class RelicBolt:MonoBehaviour
    {
        public RelicGame game;public Vector3 velocity;public float damage=12;
        float age;
        void Update()
        {
            if(game.State!=SessionState.Playing)return;
            var start=transform.position;var step=velocity*Time.deltaTime;
            if(Physics.SphereCast(start,.14f,step.normalized,out var wall,step.magnitude,1<<8,QueryTriggerInteraction.Ignore))
            {game.Burst(wall.point,new Color(1,.3f,.1f),10,2);Destroy(gameObject);return;}
            transform.position+=step;age+=Time.deltaTime;
            Vector3 target=game.hero.transform.position+Vector3.up;
            float t=step.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(target-start,step)/step.sqrMagnitude):0;
            if(Vector3.Distance(start+step*t,target)<.65f)
            {game.hero.Hurt(damage);game.Burst(transform.position,new Color(1,.2f,.1f),12,3);Destroy(gameObject);}
            if(age>5 || transform.position.magnitude>38)Destroy(gameObject);
        }
    }
}
