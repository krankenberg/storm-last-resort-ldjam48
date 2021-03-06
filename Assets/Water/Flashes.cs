using Audio;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Water
{
    public class Flashes : MonoBehaviour
    {
        public RawImage FlashImage;
        public float LowerRangeTime;
        public float UpperRangeTime;
        public float LowerRangeIntensity;
        public float UpperRangeIntensity;
        public float RampUpTime;
        public float RampDownTime;

        public AudioMixerGroup ThunderGroup;
        public AudioClip SmallThunderAudio;
        public AudioClip BigThunderAudio;
        public float SmallThunderProbability;

        private float _nextFlashTime;
        private float _intensityAim;
        private float _rampUpRemaining;
        private float _rampDownRemaining;
        private SoundManager _soundManager;

        private void Start()
        {
            _nextFlashTime = UpperRangeTime;
            _soundManager = SoundManager.FindByTag();
        }

        private void Update()
        {
            if (GlobalGameState.IsUnpaused())
            {
                if (_nextFlashTime > 0)
                {
                    _nextFlashTime -= Time.deltaTime;
                }

                if (_nextFlashTime <= 0)
                {
                    _nextFlashTime = Random.Range(LowerRangeTime, UpperRangeTime);
                    _intensityAim = Random.Range(LowerRangeIntensity, UpperRangeIntensity);
                    _rampUpRemaining = RampUpTime - _rampUpRemaining;

                    var audioClip = Random.Range(0F, 1F) < SmallThunderProbability ? SmallThunderAudio : BigThunderAudio;
                    _soundManager.PlaySound(audioClip, ThunderGroup, pitchMin: 0.8F, pitchMax: 1.2F);
                }

                float currentIntensity = 0;
                if (_rampUpRemaining > 0)
                {
                    _rampUpRemaining -= Time.deltaTime;
                    currentIntensity = Mathf.Lerp(0, _intensityAim, 1 - _rampUpRemaining / RampUpTime);

                    if (_rampUpRemaining <= 0)
                    {
                        _rampUpRemaining = 0;
                        _rampDownRemaining = RampDownTime;
                    }
                }
                else if (_rampDownRemaining > 0)
                {
                    _rampDownRemaining -= Time.deltaTime;
                    currentIntensity = Mathf.Lerp(0, _intensityAim, _rampDownRemaining / RampDownTime);

                    if (_rampDownRemaining <= 0)
                    {
                        _rampDownRemaining = 0;
                    }
                }

                FlashImage.color = new Color(1, 1, 1, currentIntensity);
            }
        }
    }
}
