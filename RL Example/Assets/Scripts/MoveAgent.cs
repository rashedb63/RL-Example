using UnityEngine;
using System;
using static Learning;

public class MoveAgent : MonoBehaviour
{
    public Transform target;
    public GameObject SuccessEffect;
    public GameObject CollisionEffect;
    public GameObject CollisionSoundEffect;
    public GameObject TargetEffect;
    private GameObject Target;
    public float Speed;
    private State startState;
    private State endState;
    public event Action<State, State, float> OnAgentComplete;

    public void Initialize(State startState, State endState, Transform target, GameObject SuccessEffect, GameObject CollisionEffect, 
        GameObject TargetEffect, GameObject Target, float Speed)
    {
        this.startState = startState;
        this.endState = endState;
        this.target = target;
        this.SuccessEffect = SuccessEffect;
        this.CollisionEffect = CollisionEffect;
        this.TargetEffect = TargetEffect;
        this.Target = Target;
        this.Speed = Speed;
    }

    void Start()
    {
        Target = Instantiate(TargetEffect, target.transform.position, Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position != target.transform.position)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                Speed * Time.deltaTime
            );
        }
        else
        {
            Instantiate(SuccessEffect, this.transform.position, Quaternion.identity);
            Destroy(Target);
            Destroy(this.gameObject);
            OnAgentComplete?.Invoke(startState, endState, 0f);
            //Debug.Log("No reward");
        }
    }
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Instantiate(CollisionEffect, this.transform.position, Quaternion.identity);
            Instantiate(CollisionSoundEffect, this.transform.position, Quaternion.identity);
            Destroy(Target);
            Destroy(this.gameObject);
            OnAgentComplete?.Invoke(startState, endState, -10f);
            //Debug.Log("Penality");
        }
        else if (collision.gameObject.CompareTag("Goal"))
        {
            Instantiate(SuccessEffect, this.transform.position, Quaternion.identity);
            Destroy(Target);
            Destroy(this.gameObject);
            OnAgentComplete?.Invoke(startState, endState, 100f);
            //Debug.Log("Goal");
        }
        else
        {
            Destroy(Target);
            Destroy(this.gameObject);
            OnAgentComplete?.Invoke(startState, endState, 0f);
            //Debug.Log("No reward");
        }
    }
}