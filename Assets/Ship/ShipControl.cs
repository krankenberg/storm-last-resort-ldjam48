using System;
using Audio;
using UI;
using UnityEngine;
using UnityEngine.Audio;

namespace Ship
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ShipControl : MonoBehaviour
    {
        public bool Invincible;
        public Team Team = Team.Enemy;
        public int SailsOpenMin = 0;
        public int SailsOpenMax = 3;
        public float SpeedUpTime = 1;
        public float BreakTime = 1;
        public float RudderPositionMin = -30;
        public float RudderPositionMax = 30;
        public float SteeringSpeed = 30;
        public float RudderDeadZone = 2.5F;
        public float MaxHullHealth = 100;
        public float MaxCrewHealth = 50;
        public float SpeedModifier = 1F;
        public float CollisionFrontAngle = 30F;
        public float MaxCollisionDamage = 20F;
        public float MinSpeedForCollisionDamage = 0.5F;
        public float HullHealthSpeedReductionDeadZone = .2F;
        public LayerMask ShipLayerMask;
        public float DamageOverTime = 0;
       

        public AudioMixerGroup PlayerAudioMixerGroup;
        public AudioMixerGroup EnemyAudioMixerGroup;
        public AudioMixerGroup RudderAudioMixerGroup;
        public AudioClip RudderAudio;
        public AudioClip StrikeSailsAudio;
        public AudioClip SetSailsAudio;
        public AudioClip CrashAudio;

        private float _rudderPosition;
        private int _sailsOpen;
        private Rigidbody2D _rigidbody;

        private float _currentVelocity;
        private Vector2 _currentVelocityVector;
        private Bars _bars;

        private float _currentHullHealth;
        private float _currentCrewHealth;

        private float _lastCrash;

        private SoundManager _soundManager;

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _bars = GetComponent<Bars>();

            if (Team == Team.Enemy)
            {
                _currentHullHealth = MaxHullHealth;
                _currentCrewHealth = MaxCrewHealth;
            }

            _soundManager = SoundManager.FindByTag();
        }

        private void Update()
        {
            if (GlobalGameState.IsUnpaused())
            {
                _bars.HullBar.Value = _currentHullHealth / MaxHullHealth;
                _bars.CrewBar.Value = _currentCrewHealth / MaxCrewHealth;
                
                _lastCrash = _lastCrash > 0 ? _lastCrash - Time.deltaTime : 0;
                if (_currentVelocity < _sailsOpen)
                {
                    _currentVelocity = Mathf.Min(_currentVelocity + Time.deltaTime * SpeedUpTime * _sailsOpen, _sailsOpen);
                }
                else
                {
                    _currentVelocity = Mathf.Max(_currentVelocity - Time.deltaTime * BreakTime, _sailsOpen);
                }

                _currentVelocityVector = transform.up * ModifiedVelocity();
                _rigidbody.velocity = _currentVelocityVector;

                if (_currentVelocity > 0.1f)
                {
                    var rudderPositionWithAppliedDeadZone = RudderPosition();
                    _rigidbody.angularVelocity = -rudderPositionWithAppliedDeadZone / SailsOpenMax * ModifiedVelocity();
                }
                else
                {
                    _rigidbody.angularVelocity = 0;
                }

                if (DamageOverTime >= 0.01F)
                {
                    ChangeHullHealth(-DamageOverTime * Time.deltaTime);
                }
            }
        }

        public void ChangeHullHealth(float healthChange)
        {
            _currentHullHealth = Mathf.Clamp(_currentHullHealth + healthChange, 0, MaxHullHealth);

            var died = false;
            if (Math.Abs(_currentHullHealth) < 0.01f)
            {
                died = true;
                _currentHullHealth = 0;
            }

            _bars.HullBar.Value = _currentHullHealth / MaxHullHealth;

            if (died)
            {
                Die();
            }
        }

        public void ChangeCrewHealth(float healthChange)
        {
            _currentCrewHealth = Mathf.Clamp(_currentCrewHealth + healthChange, 0, MaxCrewHealth);

            var died = false;
            if (Math.Abs(_currentCrewHealth) < 0.01f)
            {
                died = true;
                _currentCrewHealth = 0;
            }

            _bars.CrewBar.Value = _currentCrewHealth / MaxCrewHealth;

            if (died)
            {
                Die();
            }
        }

        private void Die()
        {
            if (!Invincible && Team == Team.Enemy)
            {
                Destroy(gameObject);
            }
        }

        public void SteerRight()
        {
            if (ChangeRudderPosition(1) && Input.GetKeyDown(KeyCode.D) && Team == Team.Player)
            {
//                Debug.Log($"Steered right, now at {_rudderPosition}.");

                _soundManager.PlaySound(RudderAudio, RudderAudioMixerGroup, pitchMin: 0.95F, pitchMax: 1.05F);
            }
        }

        public void SteerLeft()
        {
            if (ChangeRudderPosition(-1) && Input.GetKeyDown(KeyCode.A) && Team == Team.Player)
            {
//                Debug.Log($"Steered left, now at {_rudderPosition}.");

                _soundManager.PlaySound(RudderAudio, RudderAudioMixerGroup, pitchMin: 0.95F, pitchMax: 1.05F);
            }
        }

        public void OpenSail()
        {
            if (ChangeOpenSailCount(1))
            {
                _soundManager.PlaySound(SetSailsAudio, Team == Team.Player ? PlayerAudioMixerGroup : EnemyAudioMixerGroup, pitchMin: 0.95F, pitchMax: 1.05F);
//                Debug.Log($"Opened a sail, now having {_sailsOpen} opened sails.");
            }
        }

        public void CloseSail()
        {
            if (ChangeOpenSailCount(-1))
            {
                _soundManager.PlaySound(StrikeSailsAudio, Team == Team.Player ? PlayerAudioMixerGroup : EnemyAudioMixerGroup, pitchMin: 0.95F, pitchMax: 1.05F);
//                Debug.Log($"Closed a sail, now having {_sailsOpen} opened sails.");
            }
        }

        public float GetCurrentVelocity()
        {
            return ModifiedVelocity();
        }

        private float ModifiedVelocity()
        {
            return _currentVelocity * SpeedModifier * Math.Min(_currentHullHealth / MaxHullHealth + HullHealthSpeedReductionDeadZone, 1);
        }

        private bool ChangeOpenSailCount(int sailCountChange)
        {
            var newSailCount = _sailsOpen + sailCountChange;
            if (newSailCount < SailsOpenMin || newSailCount > SailsOpenMax)
            {
                return false;
            }

            _sailsOpen += sailCountChange;
            return true;
        }

        private bool ChangeRudderPosition(int direction)
        {
            var newRudderPosition = _rudderPosition + direction * SteeringSpeed * Time.deltaTime;
            if (newRudderPosition < RudderPositionMin || newRudderPosition > RudderPositionMax)
            {
                return false;
            }

            _rudderPosition = Mathf.Clamp(newRudderPosition, RudderPositionMin, RudderPositionMax);
            return true;
        }

        public static ShipControl FindShipControlInParents(GameObject otherGameObject)
        {
            var shipControl = otherGameObject.GetComponent<ShipControl>();
            if (shipControl != null)
            {
                return shipControl;
            }

            if (otherGameObject.transform.parent != null)
            {
                return FindShipControlInParents(otherGameObject.transform.parent.gameObject);
            }

            return null;
        }

        public int OpenedSails()
        {
            return _sailsOpen;
        }

        public float RudderPosition()
        {
            return Mathf.Abs(_rudderPosition) < RudderDeadZone ? 0 : _rudderPosition;
        }

        public void UpdateFromGlobalGameState()
        {
            MaxCrewHealth = GlobalGameState.MaxCrewHealth;
            MaxHullHealth = GlobalGameState.MaxHullHealth;
            _currentCrewHealth = GlobalGameState.CurrentCrewHealth;
            _currentHullHealth = GlobalGameState.CurrentHullHealth;
            
            var firingArcControls = GetComponentsInChildren<FiringArcControl>();
            foreach (var firingArcControl in firingArcControls)
            {
                firingArcControl.CannonBallCount = GlobalGameState.CannonCount;
                firingArcControl.ReadyUpTime = GlobalGameState.ReadyUpTime;
            }

            Invincible = false;
        }

        public void UpdateToGlobalGameState()
        {
            GlobalGameState.CurrentCrewHealth = Mathf.RoundToInt(_currentCrewHealth);
            GlobalGameState.CurrentHullHealth = Mathf.RoundToInt(_currentHullHealth);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0, 0, CollisionFrontAngle) * transform.up * 2);
            Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0, 0, -CollisionFrontAngle) * transform.up * 2);
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (_lastCrash <= 0 && other.collider.IsTouchingLayers(ShipLayerMask))
            {
                var otherShipControl = FindShipControlInParents(other.gameObject);
                if (otherShipControl != null)
                {
                    _lastCrash = 2.5f;
                    var directionToOtherShip = otherShipControl.transform.position - transform.position;
                    var angle = Vector2.SignedAngle(transform.up, directionToOtherShip);

                    var velocity = _rigidbody.velocity;
                    var othersVelocity = otherShipControl._rigidbody.velocity;
                    var velocityDifference = (othersVelocity - velocity).magnitude;
                    var collisionModifier = (MaxCollisionDamage) * Math.Max(0, 1 - Math.Abs(angle) / CollisionFrontAngle);
                    var collisionDamage = collisionModifier + MaxCollisionDamage *
                                          Math.Max(0, velocityDifference + MinSpeedForCollisionDamage) / (SailsOpenMax * 2 + MinSpeedForCollisionDamage);

                    Crash(collisionDamage);
                }
            }
        }

        private void Crash(float collisionDamage)
        {
            Debug.Log($"{gameObject.name} crashed into something for {collisionDamage} damage.");
                    
            _soundManager.PlaySound(CrashAudio, Team == Team.Player ? PlayerAudioMixerGroup : EnemyAudioMixerGroup, pitchMin: 0.90F, pitchMax: 1.1F);
            ChangeHullHealth(-collisionDamage);
        }

        public float CurrentCrewHealth()
        {
            return _currentCrewHealth;
        }

        public float CurrentHullHealth()
        {
            return _currentHullHealth;
        }
    }
}
