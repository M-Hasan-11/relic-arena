using UnityEngine;
namespace RelicArena
{
    // CharacterController performs swept movement; no transform-based combat displacement.
    public class ArenaMotor : MonoBehaviour
    {
        public CharacterController controller;
        public Vector3 Velocity { get; private set; }
        float vertical;
        public void Initialize(float radius=.38f,float height=1.8f)
        {
            controller=gameObject.AddComponent<CharacterController>();
            controller.radius=radius;controller.height=height;controller.center=Vector3.up*(height*.5f+.03f);
            controller.skinWidth=.025f;controller.stepOffset=.25f;controller.slopeLimit=42;controller.minMoveDistance=0;
        }
        public void Warp(Vector3 p)
        {controller.enabled=false;transform.position=p;controller.enabled=true;vertical=0;Velocity=Vector3.zero;Physics.SyncTransforms();}
        public void Move(Vector3 displacement,float dt,bool gravity=true)
        {
            if(dt<=0)return;
            var before=transform.position;
            if(gravity){vertical=controller.isGrounded?-2:Mathf.Max(-25,vertical-25*dt);displacement.y=vertical*dt;}
            var desired=transform.position+displacement;
            var flat=new Vector2(desired.x,desired.z);
            if(flat.magnitude>16.3f){flat=flat.normalized*16.3f;displacement.x=flat.x-transform.position.x;displacement.z=flat.y-transform.position.z;}
            controller.Move(displacement);Velocity=(transform.position-before)/dt;
            if(transform.position.y < -4)Warp(new Vector3(0,.15f,-5));
        }
        public Vector3 Steer(Vector3 direction)
        {
            if(direction.sqrMagnitude<.001f)return Vector3.zero;
            direction.Normalize();var origin=transform.position+Vector3.up*.7f;
            if(!Physics.SphereCast(origin,controller.radius+.08f,direction,out var hit,1.8f,1<<8,QueryTriggerInteraction.Ignore))return direction;
            var tangent=Vector3.Cross(Vector3.up,hit.normal).normalized;
            if(Vector3.Dot(tangent,direction)<0)tangent=-tangent;
            return (tangent+hit.normal*.3f).normalized;
        }
    }
}
