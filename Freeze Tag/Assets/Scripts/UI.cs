using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText = default;
    [SerializeField] private TextMeshProUGUI staminaText = default;

    private void OnEnable()
    {
        PlayerController.OnDamage += UpdateHealth;
        PlayerController.OnHeal += UpdateHealth;
        PlayerController.OnStaminaChange += UpdateStamina;
    }

    private void OnDisable()
    {
        PlayerController.OnDamage -= UpdateHealth;
        PlayerController.OnHeal -= UpdateHealth;
        PlayerController.OnStaminaChange -= UpdateStamina;
    }

    private void Start()
    {
        UpdateHealth(100);
        UpdateStamina(10);
    }

    private void UpdateHealth(float currentHealth)
    {
        healthText.text = currentHealth.ToString("0");
    }

    private void UpdateStamina(float currentStamina)
    {
        staminaText.text = currentStamina.ToString("0");
    }
}