using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerSpeed = default;
    [SerializeField] private TextMeshProUGUI isSprinting = default;

    [SerializeField] private PlayerController playerController;

    void Update()
    {
        SetSpeedValue();
        SetSprintValue();
    }

    void SetSprintValue()
    {
        if (playerController.IsSprinting)
        {
            isSprinting.text = "yes";
        }
        else
        {
            isSprinting.text = "no";
        }
    }

    void SetSpeedValue()
    {
        playerSpeed.text = playerController.currentInput.x.ToString("0");
    }
}