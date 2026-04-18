using UnityEngine;
using UnityEngine.UI;

public class KOEffectUI : MonoBehaviour
{
    [Header("=== CÀI ĐẶT HIỆU ỨNG K.O. UI ===")]
    [SerializeField] private float appearDuration = 0.35f;  
    [SerializeField] private float holdDuration = 0.9f;      
    [SerializeField] private float disappearDuration = 0.4f; 

    [SerializeField] private float startScale = 0.2f;
    [SerializeField] private float endScale = 1.35f;

    [SerializeField] private bool shake = true;
    [SerializeField] private float shakeAmount = 12f;

    private Image _image;
    private RectTransform _rect;
    private Vector2 _originalAnchoredPos;
    private float _timer = 0f;
    private float _totalTime;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rect = GetComponent<RectTransform>();
        _originalAnchoredPos = _rect.anchoredPosition;

        _totalTime = appearDuration + holdDuration + disappearDuration;

        gameObject.SetActive(false);
    }

    public void PlayKO()
    {
        gameObject.SetActive(true);
        _timer = 0f;
        _rect.localScale = Vector3.one * startScale;
        SetAlpha(0f);
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer < appearDuration)
        {
            // Phase 1: Xuất hiện
            float t = _timer / appearDuration;
            _rect.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
            SetAlpha(t);
        }
        else if (_timer < appearDuration + holdDuration)
        {
            // Phase 2: Giữ + rung
            _rect.localScale = Vector3.one * endScale;
            SetAlpha(1f);

            if (shake)
            {
                float offset = Mathf.Sin(_timer * 45f) * shakeAmount;
                _rect.anchoredPosition = _originalAnchoredPos + new Vector2(offset, 0);
            }
        }
        else
        {
            // Phase 3: Biến mất
            float t = (_timer - (appearDuration + holdDuration)) / disappearDuration;
            _rect.localScale = Vector3.one * Mathf.Lerp(endScale, endScale * 0.7f, t);
            SetAlpha(1f - t);

            if (_timer >= _totalTime)
                gameObject.SetActive(false);
        }
    }

    private void SetAlpha(float alpha)
    {
        Color c = _image.color;
        c.a = Mathf.Clamp01(alpha);
        _image.color = c;
    }
}