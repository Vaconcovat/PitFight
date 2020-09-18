using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextPopup : MonoBehaviour
{
    public string text;
    public Color color;
    public float fadeSpeed = 1;
    public Vector3 floatSpeed;

    public TextMeshProUGUI _text;

    public void Init() {
        _text.text = text;
        _text.color = color;
    }

    void Update() {
        transform.position += floatSpeed * Time.deltaTime;

        _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, _text.color.a - fadeSpeed * Time.deltaTime);
        if (_text.color.a <= 0.05f) {
            Destroy(this.gameObject);
        }
    }
}
