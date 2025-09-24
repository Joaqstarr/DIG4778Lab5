using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;

namespace Joaq.Avoidance
{
    public class Avoider : MonoBehaviour
    {

        private NavMeshAgent _navMeshAgent;
        [SerializeField] private Transform _objectToAvoid;
        [SerializeField] private float _safeDistance = 5f;
        [SerializeField] private float _avoidanceSpeed = 3.5f;
        [SerializeField] private bool _showGizmos = false;
        private PoissonDiscSampler _sampler;

        private float _poissonWidth = 10f;

        IEnumerable<Vector2> _samples;



        void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _sampler = CreateSampler();
        }

        private PoissonDiscSampler CreateSampler()
        {
            return new PoissonDiscSampler(_poissonWidth, _poissonWidth, 1f);
        }

        private void Start()
        {
            InvokeRepeating(nameof(Avoid), 0f, 0.5f);
        }

        private void Avoid()
        {
            if (Vector3.Distance(transform.position, _objectToAvoid.position) > _safeDistance) return;
            if (IsSafe(transform.position)) return;
            _samples = _sampler.Samples();
            foreach (var sample in _samples)
            {
                Vector3 potentialPosition = TransformSample(sample);
                if (IsSafe(potentialPosition))
                {
                    _navMeshAgent.speed = _avoidanceSpeed;
                    _navMeshAgent.SetDestination(potentialPosition);
                    break;
                }
            }
        }

        private Vector3 TransformSample(Vector2 sample)
        {
            Vector3 sample3 = new Vector3(sample.x, 0, sample.y);
            sample3 -= new Vector3(_poissonWidth / 2f, 0, _poissonWidth / 2f);

            return sample3 + transform.position;
        }

        private bool IsSafe(Vector3 position)
        {
            if (Vector3.Distance(position, _objectToAvoid.position) < 3) return false;

            RaycastHit hit;
            if(Physics.Raycast(position, (_objectToAvoid.position - position).normalized, out hit, 1000))
            {
                if(hit.transform == _objectToAvoid) return false;
            }

            return true;
        }


        private void OnDrawGizmos()
        {
            if(!_showGizmos) return;
            if(_sampler == null) _sampler = CreateSampler();
            if(_samples == null) _samples = _sampler.Samples();


            foreach (var sample in _samples)
            {
                Vector3 potentialPosition = TransformSample(sample);
                Gizmos.color = IsSafe(potentialPosition) ? Color.green : Color.red;
                Gizmos.DrawSphere(potentialPosition, 0.1f);
            }
        }

        private void OnValidate()
        {
            if(!GetComponent<NavMeshAgent>())
            {
                Debug.LogError("Avoider requires a NavMeshAgent component and a navmesh.", gameObject);
                return;
            }
        }
    }
}