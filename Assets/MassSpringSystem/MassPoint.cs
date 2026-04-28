using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MassPoint
{
    public Vector3 position = Vector3.zero;
    public Vector3 last_position = Vector3.zero;
    public Vector3 velocity = Vector3.zero;
    public Vector3 force = Vector3.zero;
    public Vector3 acceleration = Vector3.zero;
    public float mass = 0.5f;
    public bool isFixed = false;
    public void addForce(Vector3 force)
    {
        this.force += force;
    }

    //运算 semi-implicit Euler method
    public void Simulate(float dt)
    {
        if (isFixed)
        {
            this.force = Vector3.zero;
            return;
        }

        //自碰撞
        var colliders = Physics.OverlapSphere(position, 0.001f);
        if (colliders.Length > 0)
        {

        }
        //TODO:计算速度
        Vector3 v = (this.force / mass) * dt;
        velocity += v;
        //做碰撞
        RaycastHit hitInfo;
        if (Physics.SphereCast(position, 0.005f, velocity.normalized, out hitInfo, dt * velocity.magnitude * 1.5f))
        {
            velocity = Vector3.Reflect(velocity.normalized, hitInfo.normal) * velocity.magnitude * 0.5f * Mathf.Clamp01(hitInfo.distance * 300);
            if (velocity.magnitude < 0.01)
                velocity = Vector3.zero;
        }
        Vector3 move = Vector3.zero;
        move = velocity * dt;
        position += move;
        //velocity -= velocity * dt;
    }

    //运算 semi-implicit Euler method
    public void SimulateVerlet(float dt)
    {
        if (isFixed)
        {
            this.force = Vector3.zero;
            return;
        }

        //自碰撞
        //var colliders = Physics.OverlapSphere(position, 0.001f);
        //if (colliders.Length > 0)
        //{

        //}
        Vector3 accel = (this.force / mass);
        //TODO:计算速度
        Vector3 v = accel * dt;
        //velocity += v;
        //做碰撞
        RaycastHit hitInfo;
        if (Physics.SphereCast(position, 0.005f, velocity.normalized, out hitInfo, dt * velocity.magnitude * 1.5f))
        {
            velocity = Vector3.Reflect(velocity.normalized, hitInfo.normal) * velocity.magnitude * 0.5f * Mathf.Clamp01(hitInfo.distance * 300);
            if (velocity.magnitude < 0.01)
                velocity = Vector3.zero;
        }
        position = position + velocity * dt + 0.5f * acceleration * dt * dt;
        //下一时刻的加速度
        Vector3 accelerationNext = (this.force / mass);
        velocity = velocity + 0.5f * (accelerationNext + acceleration) * dt;
        acceleration = accelerationNext;

    }
    public void Simulate3(float dt)
    {
        if (isFixed)
        {
            this.force = Vector3.zero;
            return;
        }

        //自碰撞
        var colliders = Physics.OverlapSphere(position, 0.001f);
        if (colliders.Length > 0)
        {

        }
        Vector3 accel = (this.force / mass);
        //TODO:计算速度
        Vector3 v = accel * dt;

        velocity += v;
        //做碰撞
        RaycastHit hitInfo;
        if (Physics.SphereCast(position, 0.005f, velocity.normalized, out hitInfo, dt * velocity.magnitude * 1.5f))
        {
            velocity = Vector3.Reflect(velocity.normalized, hitInfo.normal) * velocity.magnitude * 0.5f * Mathf.Clamp01(hitInfo.distance * 300);
            if (velocity.magnitude < 0.01)
                velocity = Vector3.zero;
        }
        Vector3 move = Vector3.zero;
        move = velocity * dt;
        Vector3 newPosition = position + move;
        velocity = newPosition - position;
        position = newPosition;
        //velocity -= velocity * dt;
    }

    public void Simulate2(float dt)
    {
        Vector3 v = (this.force / mass) * dt;
        Vector3 move = (velocity + v) * dt;

        velocity += v;
        position += move;

        velocity -= velocity * dt * 0.01f;
    }

    public void SimulateVerlet2(float dt)
    {
        if (!isFixed)
        {

            force += Vector3.down * 0.98f * mass*50;

            Vector3 acceleration = (force+Vector3.one*0.00001f) / mass;

            float damping = 0.01f;

            //上一次位置
            Vector3 x_t0 = last_position;
            //当前位置
            Vector3 x_t1 = position;

  
            Vector3 x_t2 = x_t1 + (x_t1 - x_t0) * (1.0f-damping) + acceleration * dt * dt;

            position = x_t2;

            last_position = x_t1;
        }
        force = Vector3.zero;
    }
}