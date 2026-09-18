using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace RelicArena
{
    public partial class RelicGame
    {
        IEnumerator SmokeTestV3()
        {
            string dir=System.IO.Path.Combine(Application.dataPath,"..","Verification");System.IO.Directory.CreateDirectory(dir);
            var results=new List<string>();
            yield return new WaitForSeconds(1);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir,"title.png"));yield return new WaitForSeconds(.3f);
            Begin();waveDelay=999;yield return new WaitForSeconds(.3f);
            // Sample real bone movement, rather than merely checking Animator state names.
            hero.enabled=false;
            var hand=hero.animator.GetBoneTransform(HumanBodyBones.RightHand);
            foreach(string action in new[]{"Attack","Attack2","Dodge","Pulse"})
            {
                hero.animator.Play(action,0,.1f);yield return null;
                var bone=hero.transform.InverseTransformPoint(hand.position);
                hero.animator.Play(action,0,.6f);yield return null;
                Check(Vector3.Distance(bone,hero.transform.InverseTransformPoint(hand.position))>.035f,action+" animates humanoid bones",results);
            }
            hero.enabled=true;hero.ResetGuardian();
            yield return new WaitForSeconds(.2f);
            Check(hero.animator && hero.animator.avatar.isHuman,"Humanoid avatar is valid",results);
            Check(hero.animator.runtimeAnimatorController.animationClips.Any(c=>c.name.Contains("mixamo")),"Adobe Mixamo clips connected to live controller",results);
            Check(levelRoots.Length==3 && levelSkies.All(x=>x),"Three level layouts and skies connected",results);
            Check(hero.motor.controller.isGrounded,"Player grounded on arena collider",results);
            var initial=hero.transform.position;
            InputSystem.QueueStateEvent(Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.W));
            for(int i=0;i<45;i++)yield return null;
            Check(Vector3.Distance(initial,hero.transform.position)>1,"Keyboard input moves swept character controller",results);
            Check(hero.animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion") && hero.animator.GetFloat("Speed")>1,"Locomotion blend follows actual velocity",results);
            InputSystem.QueueStateEvent(Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState());
            yield return new WaitForSeconds(.3f);
            hero.motor.Warp(new Vector3(0,.1f,-3));hero.transform.rotation=Quaternion.identity;
            var target=SpawnEnemy(new Vector3(0,0,-.9f));target.enabled=false;
            var hp=target.health;hero.Strike();yield return new WaitForSeconds(.06f);
            Check(target.health==hp,"Sword damage waits for swing impact",results);
            yield return new WaitForSeconds(.2f);
            Check(target.health<hp,"Sword impact damages target",results);
            Check(hero.animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"),"Attack animation is playing",results);
            hp=hero.health;hero.Dodge(Vector3.right);hero.Hurt(30);
            Check(hero.health==hp,"Dodge grants invulnerability",results);
            yield return new WaitForSeconds(.4f);
            Check(hero.animator.GetCurrentAnimatorStateInfo(0).IsName("Dodge"),"Roll animation continues after movement burst",results);
            yield return new WaitForSeconds(.25f);hero.Hurt(12);Check(hero.health<hp,"Damage resumes after dodge",results);
            target.motor.Warp(hero.transform.position+Vector3.forward*2);hp=target.health;hero.Pulse();yield return new WaitForSeconds(.14f);
            Check(target.health<hp,"Shockwave damages enemies",results);
            Check(hero.animator.GetCurrentAnimatorStateInfo(0).IsName("Pulse"),"Shockwave casting animation plays",results);
            float cooldown=hero.pulseCooldown;hero.Pulse();Check(hero.pulseCooldown==cooldown,"Shockwave respects cooldown",results);
            Pause(true);Check(Time.timeScale==0,"Pause freezes simulation",results);Pause(false);
            Check(Time.timeScale==1,"Resume restores simulation",results);
            Begin();waveDelay=999;yield return null;
            hero.motor.Warp(new Vector3(5.65f,.1f,2.5f));
            float nearestCover=float.MaxValue,highestFoot=-100;
            for(int i=0;i<50;i++)
            {
                hero.Move(Vector3.forward,Time.deltaTime,10);yield return null;
                var p=hero.transform.position;
                nearestCover=Mathf.Min(nearestCover,Vector2.Distance(new Vector2(p.x,p.z),new Vector2(5.65685f,5.65685f)));
                highestFoot=Mathf.Max(highestFoot,p.y);
            }
            Check(nearestCover>1.3f && highestFoot<.3f,"Solid cover prevents penetration and climbing while sliding",results);
            Check(hero.transform.position.y>-.1f && hero.transform.position.y<.2f,"Gravity keeps player on ground",results);
            hero.motor.Warp(new Vector3(15.5f,.1f,0));hero.Move(Vector3.right,1,20);
            Check(new Vector2(hero.transform.position.x,hero.transform.position.z).magnitude<16.4f,"Arena boundary contains large movement",results);
            hero.motor.Warp(new Vector3(5.65f,.1f,2));
            var projectile=new GameObject("Collision test bolt");projectile.transform.position=new Vector3(5.65f,1,9);
            var shot=projectile.AddComponent<RelicBolt>();shot.game=this;shot.velocity=Vector3.back*12;
            hp=hero.health;yield return new WaitForSeconds(.8f);
            Check(!projectile && hero.health==hp,"Cover intercepts projectile before player",results);
            Begin();waveDelay=999;yield return null;hero.motor.Warp(Vector3.zero);
            var caster=SpawnEnemy(new Vector3(0,0,8));caster.ranged=true;
            hp=hero.health;yield return new WaitForSeconds(3.5f);
            Check(hero.health<hp,"Ranged AI fires damaging projectile",results);
            Check(Vector3.Distance(caster.transform.position,hero.transform.position)>5,"Ranged AI maintains distance",results);
            Begin();waveDelay=999;yield return null;hero.motor.Warp(new Vector3(0,0,-3));
            var champion=SpawnEnemy(Vector3.zero,true,true);champion.MakeBoss();hp=hero.health;
            yield return new WaitForSeconds(2.5f);
            Check(hero.health<hp,"Boss telegraph resolves into damaging slam",results);
            champion.Hurt(champion.maxHealth*.6f);Check(champion.Enraged,"Boss enters enraged phase below half health",results);
            float current=champion.health;champion.Hurt(20);Check(champion.health<current,"Boss remains damageable while enraged",results);
            OpenUpgrade();float damage=hero.swordDamage;ChooseUpgrade(0);Check(hero.swordDamage==damage+12,"Sword blessing upgrades damage",results);
            OpenUpgrade();ChooseUpgrade(1);Check(hero.maxHealth==125,"Vitality blessing upgrades cap",results);
            OpenUpgrade();ChooseUpgrade(2);Check(hero.pulseRecovery==6,"Storm blessing upgrades cooldown",results);
            Begin();yield return new WaitForSeconds(2);
            Check(wave==1 && enemies.Count==4,"Campaign starts with four enemies",results);
            int bossesDefeated=0;
            for(int encounter=1;encounter<=WaveCount;encounter++)
            {
                Check(wave==encounter && State==SessionState.Playing,"Encounter "+encounter+" progresses",results);
                if(LocalWave==3)
                {
                    Check(boss && boss.isBoss,"Level "+(Level+1)+" has a boss",results);
                    hero.health=hero.maxHealth;damageTexts.Clear();yield return new WaitForSeconds(1.4f);
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir,"level-"+(Level+1)+"-boss.png"));yield return new WaitForSeconds(.2f);
                    bossesDefeated++;
                }
                foreach(var e in enemies.ToArray())e.Hurt(10000);
                yield return null;
                if(encounter==1)
                {
                    hero.health=50;var pickup=FindAnyObjectByType<HealingRelic>();
                    Check(pickup!=null,"Third kill drops healing relic",results);
                    if(pickup)hero.motor.Warp(new Vector3(pickup.transform.position.x,.05f,pickup.transform.position.z));
                    yield return null;yield return null;Check(hero.health==70,"Healing relic restores 20 health",results);
                }
                waveDelay=0;yield return null;yield return null;
                if(encounter<WaveCount)
                {
                    Check(State==SessionState.Choosing,"Encounter "+encounter+" offers blessing",results);
                    if(encounter==1){ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir,"upgrade.png"));yield return new WaitForSecondsRealtime(.2f);}
                    ChooseUpgrade(encounter%3);waveDelay=0;yield return null;yield return null;
                    Check(levelRoots[Level].activeSelf,"Correct level layout active after transition",results);
                }
            }
            Check(State==SessionState.Won && bossesDefeated==3 && kills==45,"Nine encounters and three bosses complete campaign",results);
            Begin();waveDelay=999;hero.Hurt(10000);Check(State==SessionState.Lost,"Defeat ends session",results);
            Begin();Check(hero.health==100 && wave==0 && score==0 && levelRoots[0].activeSelf,"Restart resets campaign and upgrades",results);
            yield return new WaitForSeconds(2.5f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir,"gameplay.png"));
            yield return new WaitForSeconds(.6f);
            var frames=new List<float>();
            for(int i=0;i<120;i++){yield return null;frames.Add(Time.unscaledDeltaTime*1000);}
            frames.Sort();float mean=frames.Average();
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir,"performance.txt"),"120 rendered frames during live encounter\nMean: "+mean.ToString("0.00")+" ms\nApproximate FPS: "+(1000/mean).ToString("0.0")+"\n95th percentile: "+frames[113].ToString("0.00")+" ms\nResolution: "+Screen.width+" x "+Screen.height+"\nGPU: "+SystemInfo.graphicsDeviceName+"\nNot a full hardware benchmark.");
            System.IO.File.WriteAllLines(System.IO.Path.Combine(dir,"smoke-results.txt"),results);
            Debug.Log("RELIC_SMOKE_COMPLETE\n"+string.Join("\n",results));Application.Quit(results.Exists(x=>x.StartsWith("FAIL"))?1:0);
        }
    }
}
