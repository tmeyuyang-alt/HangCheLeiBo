using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MRope : MonoBehaviour
{
    public Transform[] Bones;

    public MassPoint[] massPointArray { get; set; }
    public Vector3[] defualtForward { get; set; }

    private List<Spring> springs = new List<Spring>();
    public float mass = 1;
    public float ks = 1;

    public bool FixedEnd;
    private Vector3 EndPos;

    public Transform Handle;
    void Start()
    {
        massPointArray = new MassPoint[Bones.Length];
        defualtForward = new Vector3[Bones.Length];


        for (int i = 0; i < massPointArray.Length; i++)
        {
            massPointArray[i] = new MassPoint();
            massPointArray[i].mass = mass;
            massPointArray[i].position = Bones[i].position;
            massPointArray[i].last_position = Bones[i].position;
            defualtForward[i] = Bones[i].forward;
        }

        EndPos = massPointArray[0].position;

        for (int i = 0; i < Bones.Length - 1; i++)
        {
            float fixedLength = Vector3.Distance(Bones[i].position, Bones[i + 1].position);
            Spring spring = new Spring(massPointArray[i], massPointArray[i + 1], fixedLength, ks);
            springs.Add(spring);
        }


        for (int i = 0; i < Bones.Length - 2; i++)
        {
            float fixedLength = Vector3.Distance(Bones[i].position, Bones[i + 2].position);

            Spring spring = new Spring(massPointArray[i], massPointArray[i + 2], fixedLength, ks*0.5f);

            springs.Add(spring);
        }

        for (int i = 0; i < Bones.Length - 3; i++)
        {
            float fixedLength = Vector3.Distance(Bones[i + 1].position, Bones[i + 2].position);

            Spring spring = new Spring(massPointArray[i + 1], massPointArray[i + 2], fixedLength, ks * 0.5f);

            springs.Add(spring);
        }
    }

    public int subStep = 2;

    void LateUpdate()
    {
        if (Handle==null)
            return;
        EndPos = massPointArray[0].position;

        for (int c = 0; c < subStep; c++)
        {
            float dt = Time.fixedDeltaTime / subStep;

            massPointArray[massPointArray.Length - 1].isFixed = true;
            massPointArray[massPointArray.Length - 1].position = Handle.position;
            massPointArray[massPointArray.Length - 1].mass = 1000;

            if (FixedEnd)
            {
                massPointArray[0].isFixed = true;
                massPointArray[0].position = EndPos;
                massPointArray[0].mass = 1000;
            }


            for (int i = 0; i < massPointArray.Length; i++)
                massPointArray[i].SimulateVerlet2(dt);

            for (int i = 0; i < springs.Count; i++)
                springs[i].Solve2();

        }

        for (int i = 0; i < Bones.Length; i++)
        {
            Bones[i].transform.position = massPointArray[i].position;
        }

        //中心点
        Vector3 center = Vector3.zero;
        for (int i = 0; i < massPointArray.Length; i++)
        {
            center += massPointArray[i].position;
        }

        List<Vector3> forwardX = new List<Vector3>();

        for (int i = 0; i < massPointArray.Length - 1; i++)
        {
            Vector3 fw = (massPointArray[i + 1].position - massPointArray[i].position).normalized;
            forwardX.Add(-fw);
        }
        forwardX.Add(forwardX[forwardX.Count - 1]);

        for (int i = 0; i < forwardX.Count - 1; i++)
        {
            //实际上时Z轴
            Vector3 forwardZ = Vector3.Normalize(Vector3.Cross(forwardX[i],  Vector3.up));

            if (Vector3.Dot(defualtForward[i], forwardZ) < 0)
            {
                forwardZ = -forwardZ;
            }

            Vector3 forwardY = Vector3.Normalize(Vector3.Cross(forwardZ, forwardX[i]));
            if(forwardY != Vector3.zero && forwardZ!=Vector3.zero)
            Bones[i].transform.rotation = Quaternion.LookRotation(forwardZ, forwardY);
        }

    }

    public void OnDrawGizmos()
    {
        if (massPointArray == null) return;

        for (int i = 0; i < Bones.Length; i++)
        {
            Gizmos.DrawSphere(Bones[i].position, 0.05f);
        }

        //中心点
        Vector3 center = Vector3.zero;
        for (int i = 0; i < massPointArray.Length; i++)
        {
            center += massPointArray[i].position;
        }
        center = center / massPointArray.Length;
        Gizmos.DrawSphere(center, 0.05f);


        List<Vector3> forwardX = new List<Vector3>();

        for (int i = 0; i < massPointArray.Length - 1; i++)
        {
            Vector3 fw = (massPointArray[i + 1].position - massPointArray[i].position).normalized;
            forwardX.Add(-fw);
        }
        forwardX.Add(forwardX[forwardX.Count - 1]);

        //

        for (int i = 0; i < massPointArray.Length; i++)
        {


            Vector3 centerDir =  Vector3.Normalize( Bones[i].position -center);

            Vector3 fZ =Vector3.Normalize( Vector3.Cross(forwardX[i], Vector3.up));

            Vector3 fY = Vector3.Normalize(Vector3.Cross(forwardX[i], fZ));


            fZ = Vector3.Dot(centerDir, fY) < 0 ? fZ : -fZ;

            fY = Vector3.Dot(centerDir, fY) < 0 ? fY : -fY;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(Bones[i].position, Bones[i].position + forwardX[i] * 0.2f);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(Bones[i].position, Bones[i].position + fZ * 0.2f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(Bones[i].position, Bones[i].position + fY * 0.2f);
        }


        
    }
}
