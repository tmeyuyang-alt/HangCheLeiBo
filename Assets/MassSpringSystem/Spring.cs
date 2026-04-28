using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spring{

    public MassPoint massPoint1;
    public MassPoint massPoint2;
    public float fixedLength = 0.5f;
    
    public float ks = 100;
    public float kd = 0.98f;
    public bool breakOff = false;
    public Spring(MassPoint m1, MassPoint m2,float fixedLength,float ks=100)
    {
        this.massPoint1 = m1;
        this.massPoint2 = m2;
        this.fixedLength = fixedLength;
        this.ks = ks;
    }

    public void Solve()
    {
        //if (breakOff)
        //    return;

        Vector3 vector = massPoint2.position - massPoint1.position;

        Vector3 force = ks * vector.normalized * (vector.magnitude - fixedLength);
        Vector3 relativeVel = massPoint1.velocity - massPoint2.velocity;
        Vector3 fd = -kd * Vector3.Dot(vector.normalized, relativeVel) * vector.normalized;
        force += fd;
        massPoint1.addForce(force);
        massPoint2.addForce(-force);

        if (vector.magnitude > fixedLength*2)
        {
            breakOff = true;
        }
        //f += Vector3.down * 9.8f * massPoint1.mass;
    }

    public void Solve2()
    {

        Vector3 vector = massPoint2.position - massPoint1.position;

        Vector3 force = ks * vector.normalized * (vector.magnitude - fixedLength);

        if (!massPoint1.isFixed)
            massPoint1.position += force * 0.5f;

        if (!massPoint2.isFixed)
            massPoint2.position -= force * 0.5f;
    }

}
