using UnityEngine;
using UnityEngine.UI;

namespace MultiCharacterSample.UI
{
    /// <summary>
    /// UI Image에서 스프라이트 시트 프레임을 순환 재생하는 간단한 애니메이션.
    /// PlayOnce로 공격 등 일회성 모션을 재생한 뒤 기본 루프로 복귀한다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UISpriteAnimation : MonoBehaviour
    {
        public Sprite[] frames;
        public float framesPerSecond = 10f;

        Image image;
        float timer;
        int index;

        Sprite[] onceFrames;
        float onceFps;
        int onceIndex;

        void Awake()
        {
            image = GetComponent<Image>();
        }

        /// <summary>프레임 세트를 교체하고 처음부터 재생한다(몬스터 교체용).</summary>
        public void SetFrames(Sprite[] newFrames)
        {
            frames = newFrames;
            index = 0;
            timer = 0f;
            onceFrames = null;
            if (image == null) image = GetComponent<Image>();
            if (frames != null && frames.Length > 0) image.sprite = frames[0];
        }

        /// <summary>일회성 모션(공격 등)을 재생한다. 끝나면 기본 루프로 돌아간다.</summary>
        public void PlayOnce(Sprite[] clip, float fps)
        {
            if (clip == null || clip.Length == 0 || fps <= 0f) return;
            if (image == null) image = GetComponent<Image>();
            onceFrames = clip;
            onceFps = fps;
            onceIndex = 0;
            timer = 0f;
            image.sprite = clip[0];
        }

        void Update()
        {
            if (onceFrames != null)
            {
                timer += Time.deltaTime;
                float onceInterval = 1f / onceFps;
                while (timer >= onceInterval)
                {
                    timer -= onceInterval;
                    onceIndex++;
                    if (onceIndex >= onceFrames.Length)
                    {
                        // 일회성 모션 종료: 기본 루프 첫 프레임으로 복귀
                        onceFrames = null;
                        index = 0;
                        if (frames != null && frames.Length > 0) image.sprite = frames[0];
                        return;
                    }
                    image.sprite = onceFrames[onceIndex];
                }
                return;
            }

            if (frames == null || frames.Length == 0 || framesPerSecond <= 0f) return;

            timer += Time.deltaTime;
            float interval = 1f / framesPerSecond;
            while (timer >= interval)
            {
                timer -= interval;
                index = (index + 1) % frames.Length;
                image.sprite = frames[index];
            }
        }
    }
}
