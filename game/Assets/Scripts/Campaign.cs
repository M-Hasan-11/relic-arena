using UnityEngine;
namespace RelicArena
{
    public partial class RelicGame
    {
        public GameObject enemyHumanoidPrefab;
        public GameObject[] levelRoots;
        public Material[] levelSkies;
        public int Level => Mathf.Clamp((Mathf.Max(1,wave)-1)/3,0,2);
        public string LevelName => new[]{"AZURE SANCTUARY","EMBER CITADEL","ASTRAL SUMMIT"}[Level];
        public int LocalWave => wave==0?0:(wave-1)%3+1;
        public Sentinel boss;
        public bool CanAttack(Sentinel caller)
        {
            int committed=0;
            foreach(var e in enemies)if(e && e!=caller && e.IsWinding)committed++;
            return committed<(Level==2?3:2);
        }
        public void ApplyLevel(int index)
        {
            if(levelRoots!=null)for(int i=0;i<levelRoots.Length;i++)if(levelRoots[i])levelRoots[i].SetActive(i==index);
            if(levelSkies!=null && index<levelSkies.Length)RenderSettings.skybox=levelSkies[index];
            RenderSettings.fogColor=new[]{new Color(.22f,.36f,.48f),new Color(.30f,.13f,.12f),new Color(.12f,.15f,.29f)}[index];
            var sun=GameObject.Find("Sun / late amber");
            if(sun){var light=sun.GetComponent<Light>();light.color=new[]{new Color(1,.88f,.72f),new Color(1,.58f,.3f),new Color(.65f,.76f,1)}[index];light.intensity=index==2?1.6f:2.2f;}
            Physics.SyncTransforms();
        }
        void CampaignHUD(float width)
        {
            Label(32,210,420,25,"LEVEL "+(Level+1)+" / 3    "+LevelName,tiny);
            if(boss && boss.health>0 && State==SessionState.Playing)
            {
                float x=width/2-240;
                Panel(new Rect(x,36,480,70),ink);
                Label(x+18,44,440,25,boss.BossName+(boss.Enraged?" / ENRAGED":""),tiny);
                Panel(new Rect(x+18,82,444,8),new Color(.2f,.12f,.15f));
                Panel(new Rect(x+18,82,444*boss.health/boss.maxHealth,8),boss.Enraged?gold:new Color(.9f,.25f,.24f));
            }
        }
    }
}
