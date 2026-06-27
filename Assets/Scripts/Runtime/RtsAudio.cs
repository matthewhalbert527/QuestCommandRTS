using UnityEngine;

namespace QuestCommandRTS
{
    public enum RtsVoiceOrder
    {
        Move,
        Attack,
        Guard,
        Harvest
    }

    public sealed class RtsAudio : MonoBehaviour
    {
        private AudioClip[] weaponClips;
        private AudioClip[] impactClips;
        private AudioClip[] explosionClips;
        private AudioClip[] moveVoiceClips;
        private AudioClip[] attackVoiceClips;
        private AudioClip[] guardVoiceClips;
        private AudioClip[] harvestVoiceClips;
        private AudioClip[] riflemanSelectClips;
        private AudioClip[] harvesterSelectClips;
        private AudioClip[] tankSelectClips;
        private AudioClip[] skyraiderSelectClips;
        private AudioClip[] orcaLifterSelectClips;
        private AudioSource voiceSource;
        private float nextVoiceTime;

        public void Initialize()
        {
            weaponClips = Resources.LoadAll<AudioClip>("Audio/Sfx/Weapons");
            impactClips = Resources.LoadAll<AudioClip>("Audio/Sfx/Impacts");
            explosionClips = Resources.LoadAll<AudioClip>("Audio/Sfx/Explosions");
            moveVoiceClips = LoadVoice("move");
            attackVoiceClips = LoadVoice("attack");
            guardVoiceClips = LoadVoice("guard");
            harvestVoiceClips = LoadVoice("harvest");
            riflemanSelectClips = LoadVoice("rifleman_select");
            harvesterSelectClips = LoadVoice("harvester_select");
            tankSelectClips = LoadVoice("tank_select");
            skyraiderSelectClips = LoadVoice("skyraider_select");
            orcaLifterSelectClips = LoadVoice("orcalifter_select");

            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0f;
            voiceSource.volume = 0.82f;
        }

        public void PlayWeapon(Vector3 point, UnitKind kind)
        {
            UnitKind normalized = RtsBalance.NormalizeUnitKind(kind);
            float volume = RtsBalance.IsTank(normalized) || normalized == UnitKind.OrcaLifter ? 0.62f : 0.5f;
            PlaySpatial(Pick(weaponClips), point, volume, Random.Range(0.92f, 1.08f));
        }

        public void PlayWeapon(Vector3 point, RtsProjectileKind kind)
        {
            float volume = kind == RtsProjectileKind.Rocket || kind == RtsProjectileKind.TankShell || kind == RtsProjectileKind.DefenseShell ? 0.62f : 0.5f;
            PlaySpatial(Pick(weaponClips), point, volume, Random.Range(0.92f, 1.08f));
        }

        public void PlayImpact(Vector3 point)
        {
            PlaySpatial(Pick(impactClips), point, 0.42f, Random.Range(0.88f, 1.12f));
        }

        public void PlayExplosion(Vector3 point)
        {
            PlaySpatial(Pick(explosionClips), point, 0.88f, Random.Range(0.88f, 1.04f));
        }

        public void PlayUnitSelected(UnitKind kind)
        {
            AudioClip[] clips;
            switch (kind)
            {
                case UnitKind.Harvester:
                    clips = harvesterSelectClips;
                    break;
                case UnitKind.Tank:
                case UnitKind.LightTank:
                case UnitKind.MediumTank:
                case UnitKind.HeavyTank:
                case UnitKind.Humvee:
                case UnitKind.Apc:
                    clips = tankSelectClips;
                    break;
                case UnitKind.Skyraider:
                    clips = skyraiderSelectClips;
                    break;
                case UnitKind.OrcaLifter:
                    clips = orcaLifterSelectClips;
                    break;
                default:
                    clips = riflemanSelectClips;
                    break;
            }

            PlayVoice(Pick(clips));
        }

        public void PlayOrder(RtsVoiceOrder order)
        {
            AudioClip[] clips;
            switch (order)
            {
                case RtsVoiceOrder.Attack:
                    clips = attackVoiceClips;
                    break;
                case RtsVoiceOrder.Guard:
                    clips = guardVoiceClips;
                    break;
                case RtsVoiceOrder.Harvest:
                    clips = harvestVoiceClips;
                    break;
                default:
                    clips = moveVoiceClips;
                    break;
            }

            PlayVoice(Pick(clips));
        }

        private AudioClip[] LoadVoice(string prefix)
        {
            AudioClip[] selectClips = Resources.LoadAll<AudioClip>("Audio/Voice/Select");
            AudioClip[] orderClips = Resources.LoadAll<AudioClip>("Audio/Voice/Order");
            int count = CountMatches(selectClips, prefix) + CountMatches(orderClips, prefix);
            AudioClip[] matches = new AudioClip[count];
            int index = 0;
            CopyMatches(selectClips, prefix, matches, ref index);
            CopyMatches(orderClips, prefix, matches, ref index);
            return matches;
        }

        private static int CountMatches(AudioClip[] clips, string prefix)
        {
            int count = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].name.StartsWith(prefix))
                {
                    count++;
                }
            }

            return count;
        }

        private static void CopyMatches(AudioClip[] source, string prefix, AudioClip[] target, ref int targetIndex)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != null && source[i].name.StartsWith(prefix))
                {
                    target[targetIndex] = source[i];
                    targetIndex++;
                }
            }
        }

        private void PlayVoice(AudioClip clip)
        {
            if (clip == null || voiceSource == null || Time.time < nextVoiceTime)
            {
                return;
            }

            nextVoiceTime = Time.time + 0.35f;
            voiceSource.pitch = Random.Range(0.96f, 1.04f);
            voiceSource.PlayOneShot(clip);
        }

        private static AudioClip Pick(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            return clips[Random.Range(0, clips.Length)];
        }

        private static void PlaySpatial(AudioClip clip, Vector3 point, float volume, float pitch)
        {
            if (clip == null)
            {
                return;
            }

            GameObject soundObject = new GameObject("One Shot Audio");
            soundObject.transform.position = point;
            AudioSource source = soundObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 0.72f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 5f;
            source.maxDistance = 55f;
            source.Play();
            float lifetime = clip.length / Mathf.Max(0.1f, Mathf.Abs(pitch)) + 0.1f;
            if (Application.isPlaying)
            {
                Destroy(soundObject, lifetime);
            }
            else
            {
                DestroyImmediate(soundObject);
            }
        }
    }
}
