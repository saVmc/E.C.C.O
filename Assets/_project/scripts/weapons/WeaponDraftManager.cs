using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WeaponDraftManager : MonoBehaviour
{
    [SerializeField] private WeaponCatalog catalog;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private int choiceCount = 3;
    [SerializeField] private bool generateOnStart = true;

    private readonly List<GunProfile> currentChoices = new List<GunProfile>();

    public event Action<IReadOnlyList<GunProfile>> OnChoicesGenerated;

    public IReadOnlyList<GunProfile> CurrentChoices => currentChoices;

    private void Start()
    {
        if (generateOnStart)
            GenerateChoices();
    }

    public IReadOnlyList<GunProfile> GenerateChoices()
    {
        currentChoices.Clear();

        if (catalog != null)
            currentChoices.AddRange(catalog.GetRandomChoices(choiceCount));

        OnChoicesGenerated?.Invoke(currentChoices);
        return currentChoices;
    }

    public void ChooseWeapon(int index)
    {
        if (index < 0 || index >= currentChoices.Count)
            return;

        if (playerShooter == null)
            return;

        playerShooter.EquipWeapon(currentChoices[index]);
    }

    public void GrantUpgrade(GunUpgrade upgrade)
    {
        if (playerShooter == null)
            return;

        playerShooter.ApplyUpgrade(upgrade);
    }
}