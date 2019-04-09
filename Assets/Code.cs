using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Code : MonoBehaviour
{
public Slider slider;

private void Update() {
    GetComponent<Text>().text = slider.value.ToString();
}
}
