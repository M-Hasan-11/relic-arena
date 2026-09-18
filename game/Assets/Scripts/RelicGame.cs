using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RelicArena
{
    public enum SessionState { Title, Playing, Paused, Choosing, Won, Lost }

    public partial class RelicGame : MonoBehaviour
    {
        public GameObject guardianPrefab, sentinelPrefab;
        public Material tealFx, redFx, goldFx;
        public Camera view;
        public SessionState State { get; private set; }
        public readonly List<Sentinel> enemies = new List<Sentinel>();
        public Guardian hero;
        public int wave, kills, score;
        public float elapsed, waveDelay;
        public const int WaveCount = 9;
        public string announcement;
        float announceUntil, cameraYaw, cameraPitch = 26, shake;
        Vector3 cameraVelocity;
        AudioSource audioSource;
        readonly Dictionary<string, AudioClip> sounds = new Dictionary<string, AudioClip>();
        GUIStyle tiny, body, heading, title, button, number;
        Texture2D white;
        readonly Color ink = new Color(.025f,.045f,.062f,.94f);
        readonly Color mint = new Color(.32f,.95f,.81f);
        readonly Color gold = new Color(.9f,.68f,.35f);
        bool smoke;

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            var renderPipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if(renderPipeline)
            {
                renderPipeline.msaaSampleCount=2;
                renderPipeline.renderScale=SystemInfo.graphicsDeviceName.IndexOf("Intel",StringComparison.OrdinalIgnoreCase)>=0?.85f:1;
                renderPipeline.shadowDistance=40;
            }
            var backdrop=GameObject.Find("Realm horizon / Blender");
            if(backdrop)foreach(var r in backdrop.GetComponentsInChildren<Renderer>())
            {r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;}
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0;
            LoadPreferences();
            MakeSound("slash", 370, .12f, .20f);
            MakeSound("hit", 130, .15f, .22f);
            MakeSound("dash", 560, .14f, .12f);
            MakeSound("pulse", 90, .55f, .25f);
            MakeSound("collect", 780, .22f, .14f);
            MakeSound("wave", 280, .45f, .13f);
            MakeSound("hurt", 72, .25f, .20f);
            State = SessionState.Title;
            SpawnHero();
            smoke = Array.IndexOf(Environment.GetCommandLineArgs(), "-relicSmoke") >= 0;
            if (smoke) StartCoroutine(SmokeTestV3());
        }

        void SpawnHero()
        {
            var root = new GameObject("Player");
            root.transform.position = new Vector3(0,0,-5);
            var art = Instantiate(guardianPrefab, root.transform);
            art.name = "Guardian Art";
            hero = root.AddComponent<Guardian>();
            hero.Setup(this, art.transform);
        }

        public void Begin()
        {
            foreach (var e in enemies.ToArray()) if (e) Destroy(e.gameObject);
            enemies.Clear();
            foreach (var pickup in FindObjectsByType<HealingRelic>()) Destroy(pickup.gameObject);
            hero.ResetGuardian();
            ApplyLevel(0);boss=null;damageTexts.Clear();
            upgradeChosen=false;
            foreach(var bolt in FindObjectsByType<RelicBolt>())Destroy(bolt.gameObject);
            wave = kills = score = 0; elapsed = 0; waveDelay = 1.5f;
            cameraYaw = 0; cameraPitch = 27;
            State = SessionState.Playing;
            Time.timeScale = 1;
            LockCursor(true);
            Announce("THE TRIAL BEGINS", 2);
        }

        void Update()
        {
            UpdatePreferencesInput();
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (State == SessionState.Playing) Pause(true);
                else if (State == SessionState.Paused) Pause(false);
            }
            if (State == SessionState.Title && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) Begin();
            if ((State == SessionState.Lost || State == SessionState.Won) && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Begin();
            if (State != SessionState.Playing) return;
            elapsed += Time.deltaTime;
            if (enemies.Count == 0)
            {
                waveDelay -= Time.deltaTime;
                if (waveDelay <= 0)
                {
                    if (wave >= WaveCount) End(true);
                    else if(wave>0 && !upgradeChosen)OpenUpgrade();
                    else SpawnWave();
                }
            }
        }

        public void SpawnWave()
        {
            upgradeChosen=false;
            wave++;
            if(LocalWave==1)
            {
                ApplyLevel(Level);hero.motor.Warp(new Vector3(0,.1f,-5));
                hero.health=hero.maxHealth;
                foreach(var bolt in FindObjectsByType<RelicBolt>())Destroy(bolt.gameObject);
                foreach(var pickup in FindObjectsByType<HealingRelic>())Destroy(pickup.gameObject);
            }
            int count=LocalWave==3?2+Level:3+Level+LocalWave;
            for(int i=0;i<count;i++)
            {
                float a=i*Mathf.PI*2/count+wave*.65f;
                var enemy=SpawnEnemy(new Vector3(Mathf.Sin(a)*14,0,Mathf.Cos(a)*14),Level>0 && i==0);
                if(i==count-1 || (Level==2 && i==1))enemy.ranged=true;
            }
            if(LocalWave==3)
            {
                var champion=SpawnEnemy(new Vector3(0,0,11),true,true);
                champion.MakeBoss();Announce(champion.BossName+" / BOSS AWAKENS",3);
            }
            else Announce("LEVEL "+(Level+1)+" / "+LevelName+" / ENCOUNTER "+LocalWave,3);
            Sound("wave");
        }

        public Sentinel SpawnEnemy(Vector3 position, bool elite = false, bool bossModel = false)
        {
            var root = new GameObject(elite ? "Elite Sentinel" : "Sentinel");
            root.transform.position = position;
            var art = Instantiate(bossModel?sentinelPrefab:enemyHumanoidPrefab?enemyHumanoidPrefab:sentinelPrefab, root.transform);
            if (bossModel) art.transform.localScale *= 1.75f;
            else if (elite) art.transform.localScale *= 1.15f;
            var enemy = root.AddComponent<Sentinel>();
            enemy.Setup(this, art.transform, elite);
            enemies.Add(enemy);
            Ring(position + Vector3.up*.06f, 1.3f, redFx, .6f);
            return enemy;
        }

        public void EnemyFell(Sentinel e)
        {
            if (!enemies.Remove(e)) return;
            kills++; score += e.isBoss ? 1500 : e.elite ? 300 : 100;
            if (kills % 3 == 0) CreateRelic(e.transform.position);
            if (enemies.Count == 0)
            {
                waveDelay = 3.0f;
                hero.health = Mathf.Min(hero.maxHealth, hero.health + 15);
                if (wave < WaveCount) Announce("WAVE CLEARED / +15 VITALITY", 2.7f);
            }
        }

        void CreateRelic(Vector3 p)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube);
            o.name = "Healing relic"; o.transform.position = p + Vector3.up*.75f;
            o.transform.localScale = Vector3.one*.35f;
            o.transform.rotation = Quaternion.Euler(45,45,45);
            Destroy(o.GetComponent<Collider>()); o.GetComponent<Renderer>().sharedMaterial = tealFx;
            o.AddComponent<HealingRelic>().game = this;
        }

        public void End(bool won)
        {
            State = won ? SessionState.Won : SessionState.Lost;
            LockCursor(false);
            if (won) { score += Mathf.RoundToInt(hero.health)*10; Sound("collect"); }
            RecordBest();
        }
        public void Pause(bool pause)
        {
            State = pause ? SessionState.Paused : SessionState.Playing;
            Time.timeScale = pause ? 0 : 1;
            LockCursor(!pause);
        }
        public void ReturnToTitle()
        {
            Time.timeScale = 1; State = SessionState.Title; LockCursor(false);
            foreach (var e in enemies.ToArray()) if(e) Destroy(e.gameObject);
            enemies.Clear(); hero.ResetGuardian();ApplyLevel(0);
            foreach(var bolt in FindObjectsByType<RelicBolt>())Destroy(bolt.gameObject);
        }
        static void LockCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
        void OnApplicationFocus(bool focus) { if (!focus && State == SessionState.Playing && !smoke) Pause(true); }
        public void Announce(string text, float duration) { announcement = text; announceUntil = Time.time + duration; }
        public void Shake(float amount) { shake = Mathf.Max(shake,amount); }

        void LateUpdate()
        {
            if (!view || !hero) return;
            if (State == SessionState.Title)
            {
                float a = Time.time * .055f;
                view.transform.position = new Vector3(Mathf.Sin(a)*24,14,Mathf.Cos(a)*-24);
                view.transform.LookAt(new Vector3(0,1,0)); return;
            }
            if (State == SessionState.Playing && Mouse.current != null && !smoke)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                cameraYaw += delta.x * sensitivity;
                cameraPitch = Mathf.Clamp(cameraPitch - delta.y*sensitivity*.77f, 16, 58);
                cameraDistance=Mathf.Clamp(cameraDistance-Mouse.current.scroll.ReadValue().y*.005f,6,12);
            }
            var target = hero.transform.position + Vector3.up*1.35f;
            var rotation = Quaternion.Euler(cameraPitch,cameraYaw,0);
            var desired = target - rotation*Vector3.forward*cameraDistance;
            var sight=desired-target;
            if(Physics.SphereCast(target,.28f,sight.normalized,out var obstruction,sight.magnitude,1<<8,QueryTriggerInteraction.Ignore))
                desired=target+sight.normalized*Mathf.Max(1.2f,obstruction.distance-.15f);
            view.transform.position = Vector3.SmoothDamp(view.transform.position,desired,ref cameraVelocity,.10f,100,Time.unscaledDeltaTime);
            var actualSight=view.transform.position-target;
            if(Physics.SphereCast(target,.25f,actualSight.normalized,out var cameraHit,actualSight.magnitude,1<<8,QueryTriggerInteraction.Ignore))
                view.transform.position=target+actualSight.normalized*Mathf.Max(.6f,cameraHit.distance-.1f);
            view.transform.rotation = Quaternion.LookRotation(target-view.transform.position);
            if (shake > 0 && State == SessionState.Playing)
            {
                view.transform.position += UnityEngine.Random.insideUnitSphere * shake;
                shake = Mathf.MoveTowards(shake,0,Time.deltaTime*1.3f);
            }
        }

        public Vector3 CameraDirection(Vector2 input)
        {
            return Quaternion.Euler(0,cameraYaw,0) * new Vector3(input.x,0,input.y);
        }

        public void Ring(Vector3 p, float radius, Material material, float duration, bool expand = true)
        {
            var o = new GameObject("Energy ring"); o.transform.position = p;
            var line = o.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.useWorldSpace = false;
            line.loop = true; line.positionCount = 65; line.widthMultiplier = .075f;
            for (int i=0;i<65;i++) { float a=i*Mathf.PI*2/64; line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius)); }
            var effect = o.AddComponent<RelicEffect>(); effect.life = duration; effect.expand = expand;
        }
        public void Sparks(Vector3 p, Material mat, int amount = 10)
        {
            if(particleMaterial){Burst(p,mat==redFx?new Color(1,.18f,.08f):new Color(1,.7f,.2f),amount*2,3.5f);return;}
            for (int i=0;i<amount;i++)
            {
                var o = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(o.GetComponent<Collider>());
                o.name = "Spark"; o.transform.position=p; o.transform.localScale=Vector3.one*UnityEngine.Random.Range(.06f,.16f);
                o.GetComponent<Renderer>().sharedMaterial=mat;
                var fx=o.AddComponent<RelicEffect>(); fx.life=.35f; fx.velocity=UnityEngine.Random.insideUnitSphere*4+Vector3.up*2;
            }
        }
        void MakeSound(string id, float frequency, float duration, float volume)
        {
            int count = (int)(22050*duration); var data = new float[count];
            for(int i=0;i<count;i++)
            {
                float t=i/22050f, envelope=Mathf.Pow(1-i/(float)count,2);
                data[i]=(Mathf.Sin(t*frequency*Mathf.PI*2*(1-t*.6f))+.25f*Mathf.Sin(t*frequency*Mathf.PI*4))*envelope*volume;
            }
            var clip=AudioClip.Create(id,count,1,22050,false);clip.SetData(data,0);sounds[id]=clip;
        }
        public void Sound(string id) { if(sounds.TryGetValue(id,out var clip)) audioSource.PlayOneShot(clip); }

        void Styles()
        {
            if (white) return;
            white=Texture2D.whiteTexture;
            tiny=new GUIStyle(GUI.skin.label){fontSize=12,fontStyle=FontStyle.Bold};tiny.normal.textColor=new Color(.53f,.68f,.70f);
            body=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true};body.normal.textColor=new Color(.77f,.85f,.84f);
            heading=new GUIStyle(body){fontSize=27,fontStyle=FontStyle.Bold};heading.normal.textColor=Color.white;
            title=new GUIStyle(heading){fontSize=70};title.normal.textColor=new Color(.93f,.91f,.80f);
            number=new GUIStyle(heading){fontSize=38};number.normal.textColor=mint;
            button=new GUIStyle(GUI.skin.button){fontSize=16,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            button.normal.background=white;button.hover.background=white;button.active.background=white;
            button.normal.textColor=ink;button.hover.textColor=ink;button.active.textColor=ink;
        }
        void Panel(Rect r, Color c) { GUI.color=c;GUI.DrawTexture(r,white);GUI.color=Color.white; }
        bool Button(Rect r,string text) { GUI.backgroundColor=mint;bool hit=GUI.Button(r,text,button);GUI.backgroundColor=Color.white;return hit; }
        void Label(float x,float y,float w,float h,string text,GUIStyle style) { GUI.Label(new Rect(x,y,w,h),text,style); }

        void OnGUI()
        {
            Styles();
            float scale=Screen.height/900f, width=Screen.width/scale;
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));
            if(State==SessionState.Choosing){UpgradeGUI(width);return;}
            if(State==SessionState.Title)
            {
                Panel(new Rect(0,0,560,900),new Color(.018f,.034f,.047f,.95f));
                Panel(new Rect(60,72,42,3),mint);
                Label(60,94,400,25,"A GUARDIAN'S LAST TRIAL",tiny);
                Label(54,163,500,95,"RELIC",title);Label(54,240,500,95,"ARENA",title);
                Label(60,368,400,90,"The temple has awakened.\nConquer three realms and defeat their guardians.",body);
                Label(60,492,380,30,"MOVE. STRIKE. SURVIVE.",tiny);
                Label(60,536,440,85,"WASD   Move      MOUSE   Look\nLMB / J   Sword      SPACE   Dodge\nQ   Shockwave      ESC   Pause",body);
                if(Button(new Rect(60,672,365,58),"ENTER THE ARENA    /    ENTER")) Begin();
                Label(60,780,420,30,"PERSONAL BEST   /   "+bestScore.ToString("00000"),tiny);
                Label(60,818,420,30,"03 / REALMS     •     MIXAMO + FREE ASSETS",tiny);
                Label(width-280,830,230,25,"UNITY × BLENDER",tiny);
                return;
            }
            Panel(new Rect(28,25,312,108),ink);Panel(new Rect(28,25,3,108),mint);
            Label(48,37,270,25,"GUARDIAN / VITALITY",tiny);
            Label(48,60,270,40,Mathf.CeilToInt(hero.health)+" / "+hero.maxHealth,heading);
            Panel(new Rect(48,108,270,5),new Color(.2f,.26f,.28f));Panel(new Rect(48,108,270*hero.health/hero.maxHealth,5),mint);
            Panel(new Rect(width-235,25,207,108),ink);
            Label(width-215,38,170,25,"ENCOUNTER / 3",tiny);
            Label(width-215,62,90,50,LocalWave.ToString("00"),number);
            Label(width-135,78,100,25,enemies.Count+" HOSTILES",tiny);
            Label(32,152,300,28,"SCORE  "+score.ToString("00000"),tiny);
            Label(32,181,300,28,"BEST    "+bestScore.ToString("00000"),tiny);
            if(State==SessionState.Playing && Time.time<announceUntil)
            {
                var centered=new GUIStyle(heading){alignment=TextAnchor.MiddleCenter,fontSize=22};
                Panel(new Rect(width/2-340,151,680,52),ink);
                Label(width/2-330,160,660,40,announcement,centered);
            }
            Panel(new Rect(width/2-285,801,570,68),ink);
            Label(width/2-265,815,190,25,"LMB   SWORD",tiny);
            Label(width/2-80,815,190,25,"SPACE   DODGE",tiny);
            Label(width/2+115,815,160,25,"Q   SHOCKWAVE",tiny);
            Label(width/2-265,837,170,25,"ARC / CLOSE RANGE",tiny);
            Label(width/2-80,837,170,25,hero.dashCooldown<=0?"READY":hero.dashCooldown.ToString("0.0")+"s",tiny);
            Label(width/2+115,837,150,25,hero.pulseCooldown<=0?"READY":hero.pulseCooldown.ToString("0.0")+"s",tiny);
            if(State==SessionState.Playing)
            {
                Panel(new Rect(width/2-2,450-2,4,4),new Color(1,1,1,.5f));
                foreach(var e in enemies)
                {
                    if(!e)continue;var p=view.WorldToScreenPoint(e.transform.position+Vector3.up*(e.elite?3.2f:2.65f));
                    if(p.z<=0)continue;float px=p.x/scale,py=(Screen.height-p.y)/scale;
                    Panel(new Rect(px-27,py,54,4),ink);Panel(new Rect(px-27,py,54*e.health/e.maxHealth,4),e.IsWinding?gold:new Color(.9f,.25f,.24f));
                }
            }
            if(State==SessionState.Paused || State==SessionState.Won || State==SessionState.Lost)
            {
                Panel(new Rect(0,0,width,900),new Color(.01f,.02f,.03f,.74f));
                float x=width/2-260;
                Panel(new Rect(x,220,520,455),ink);Panel(new Rect(x,220,520,3),mint);
                Label(x+42,255,440,25,"RELIC ARENA / THE SKY TEMPLE",tiny);
                Label(x+42,302,440,60,State==SessionState.Paused?"TRIAL PAUSED":State==SessionState.Won?"TEMPLE RESTORED":"GUARDIAN FALLEN",heading);
                Label(x+42,379,440,85,State==SessionState.Paused?"Take a breath. Your trial will wait.":"Score  "+score+"     /     Defeated  "+kills+"\nTime  "+TimeSpan.FromSeconds(elapsed).ToString(@"mm\:ss"),body);
                if(Button(new Rect(x+42,493,436,55),State==SessionState.Paused?"RESUME":"TRY AGAIN    /    R")) { if(State==SessionState.Paused)Pause(false);else Begin(); }
                if(GUI.Button(new Rect(x+42,573,436,45),"RETURN TO TITLE"))ReturnToTitle();
                if(State==SessionState.Paused)PreferencesGUI(width);
            }
            CampaignHUD(width);
            DrawDamageNumbers(scale);
        }

        IEnumerator SmokeTest()
        {
            string directory=System.IO.Path.Combine(Application.dataPath,"..","Verification");
            System.IO.Directory.CreateDirectory(directory);
            var results=new List<string>();
            yield return new WaitForSeconds(1);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"title.png"));
            yield return new WaitForSeconds(.5f);
            Begin(); waveDelay=999;
            yield return new WaitForSeconds(.4f);
            Check(hero.animator && hero.animator.avatar && hero.animator.avatar.isHuman && hero.animator.runtimeAnimatorController,"Asset Store humanoid and animation controller",results);
            Check(particleMaterial && particleMaterial.mainTexture,"Unity Particle Pack texture connected",results);
            var initial=hero.transform.position;
            InputSystem.QueueStateEvent(Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.W));
            for(int i=0;i<30;i++)yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState());
            Check(Vector3.Distance(initial,hero.transform.position)>1,"Keyboard-driven camera-relative movement",results);
            yield return null;
            hero.transform.position=new Vector3(0,0,-3);
            hero.transform.rotation=Quaternion.identity;
            var target=SpawnEnemy(new Vector3(0,0,-.8f));
            yield return null;
            hero.Strike();yield return new WaitForSeconds(.25f);
            Check(target.health<target.maxHealth,"Sword damages a nearby sentinel",results);
            float hp=hero.health;hero.Dodge(Vector3.right);hero.Hurt(15);
            Check(hero.health==hp,"Dodge invulnerability",results);
            yield return new WaitForSeconds(.5f);hero.Hurt(12);
            Check(hero.health<hp,"Enemy damage reaches player",results);
            hero.pulseCooldown=0; target.transform.position=hero.transform.position+Vector3.forward*2;
            float before=target.health;hero.Pulse();
            Check(!target || target.health<before,"Shockwave damages sentinel",results);
            float cooldown=hero.pulseCooldown;hero.Pulse();
            Check(hero.pulseCooldown==cooldown,"Shockwave cannot bypass cooldown",results);
            hero.transform.position=new Vector3(16,0,0);hero.Move(Vector3.right,1,20);
            Check(hero.transform.position.magnitude<=16.51f,"Arena boundary contains player",results);
            Pause(true);Check(State==SessionState.Paused && Time.timeScale==0,"Pause",results);
            Pause(false);Check(State==SessionState.Playing && Time.timeScale==1,"Resume",results);
            Begin();waveDelay=999;hero.transform.position=Vector3.zero;
            var caster=SpawnEnemy(new Vector3(0,0,6));caster.ranged=true;
            hp=hero.health;yield return new WaitForSeconds(3);
            Check(hero.health<hp,"Ranged sentinel projectile hits player",results);
            OpenUpgrade();
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"upgrade.png"));
            yield return new WaitForSecondsRealtime(.3f);
            float sword=hero.swordDamage;ChooseUpgrade(0);
            Check(hero.swordDamage==sword+12 && State==SessionState.Playing,"Sword blessing applies and resumes play",results);
            OpenUpgrade();ChooseUpgrade(1);
            Check(hero.maxHealth==125 && hero.health>hp,"Vitality blessing raises health cap",results);
            OpenUpgrade();ChooseUpgrade(2);
            Check(hero.pulseRecovery==6,"Storm blessing reduces cooldown",results);
            Begin();yield return new WaitForSeconds(2);
            Check(wave==1 && enemies.Count==3,"First wave spawns three enemies",results);
            foreach(var e in enemies.ToArray())e.Hurt(1000);
            hero.health=50;
            var healing=FindAnyObjectByType<HealingRelic>();
            Check(healing!=null,"Every third kill drops healing",results);
            if(healing){hero.transform.position=new Vector3(healing.transform.position.x,0,healing.transform.position.z);yield return null;yield return null;}
            Check(hero.health==70,"Healing relic restores health",results);
            for(int expected=2;expected<=5;expected++)
            {
                waveDelay=0;yield return null;yield return null;
                if(State==SessionState.Choosing)ChooseUpgrade(0);
                waveDelay=0;yield return null;yield return null;
                Check(wave==expected && State==SessionState.Playing,"Wave "+expected+" begins after blessing",results);
                foreach(var e in enemies.ToArray())e.Hurt(1000);
            }
            waveDelay=0;yield return null;yield return null;
            Check(State==SessionState.Won && kills==25,"Five-wave victory",results);
            Begin();waveDelay=999;hero.Hurt(1000);
            Check(State==SessionState.Lost,"Defeat",results);
            Begin();Check(State==SessionState.Playing && hero.health==100 && score==0,"Restart resets session",results);
            yield return new WaitForSeconds(2);
            for(int i=0;i<90;i++)yield return null;
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"gameplay.png"));
            yield return new WaitForSeconds(.7f);
            System.IO.File.WriteAllLines(System.IO.Path.Combine(directory,"smoke-results.txt"),results);
            Debug.Log("RELIC_SMOKE_COMPLETE\n"+string.Join("\n",results));
            Application.Quit(results.Exists(s=>s.StartsWith("FAIL"))?1:0);
        }
        void Check(bool condition,string name,List<string> results) {results.Add((condition?"PASS ":"FAIL ")+name);}
    }

    public class RelicEffect : MonoBehaviour
    {
        public float life=.4f;public bool expand;public Vector3 velocity;
        float age;Vector3 initial;
        void Start(){initial=transform.localScale;}
        void Update(){age+=Time.deltaTime;transform.position+=velocity*Time.deltaTime;float t=age/life;transform.localScale=initial*(expand?Mathf.Lerp(.3f,1.35f,t):Mathf.Max(0,1-t));if(age>=life)Destroy(gameObject);}
    }
    public class HealingRelic : MonoBehaviour
    {
        public RelicGame game;float age;
        void Update()
        {
            if(game.State!=SessionState.Playing)return;
            age+=Time.deltaTime;transform.Rotate(0,80*Time.deltaTime,0,Space.World);
            if(Vector3.Distance(transform.position,game.hero.transform.position+Vector3.up*.75f)<1.4f)
            {game.hero.health=Mathf.Min(game.hero.maxHealth,game.hero.health+20);game.Sound("collect");game.Sparks(transform.position,game.tealFx);Destroy(gameObject);}
            if(age>25)Destroy(gameObject);
        }
    }
}
