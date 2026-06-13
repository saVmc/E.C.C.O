using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
public sealed class PlayerShooter : MonoBehaviour
{
    [SerializeField] private Gun activeGun;
    [SerializeField] private GunProfile startingProfile;
    [SerializeField] private bool applyStartingProfileOnStart = true;

    private bool firedThisFrame;
    private bool wasRecentlyFired = false;
    private float fireCooldownTimer = 0f;
    private const float VISIBILITY_COOLDOWN = 0.2f;

    public event Action OnShotFired;

    private void Awake()
    {
        if (activeGun == null)
            activeGun = GetComponentInChildren<Gun>();

        if (activeGun != null)
            activeGun.OnShotFired += HandleShotFired;
    }

    private void Start()
    {
        if (applyStartingProfileOnStart && activeGun != null && startingProfile != null)
            activeGun.ApplyProfile(startingProfile);

        // Initially show the gun when idle
        if (activeGun != null)
            activeGun.ShowGun();
    }

    private void Update()
    {
        firedThisFrame = false;

        if (fireCooldownTimer > 0f)
        {
            fireCooldownTimer -= Time.deltaTime;
        }

        bool isMoving = false;
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            isMoving = playerMovement.GetMovementDirection().sqrMagnitude > 0.0001f;
        }

        if (isMoving && !wasRecentlyFired && activeGun != null)
        {
            activeGun.HideGun();
        }
        else if ((!isMoving || wasRecentlyFired) && activeGun != null)
        {
            activeGun.ShowGun();
        }

        if (wasRecentlyFired && fireCooldownTimer <= 0f)
        {
            wasRecentlyFired = false;
        }

        if (activeGun == null)
            return;

        bool firePressed = false;
        bool reloadPressed = false;

        if (Keyboard.current != null)
        {
            firePressed |= Keyboard.current.spaceKey.isPressed;
            reloadPressed |= Keyboard.current.rKey.wasPressedThisFrame;
        }

        if (Mouse.current != null)
        {
            firePressed |= Mouse.current.leftButton.isPressed;
        }

        activeGun.HandleInput(firePressed, reloadPressed);
    }

    private void HandleShotFired()
    {
        firedThisFrame = true;
        wasRecentlyFired = true;
        fireCooldownTimer = VISIBILITY_COOLDOWN;

        // Ensure gun is visible when shooting
        if (activeGun != null)
        {
            activeGun.ShowGun();
        }

        OnShotFired?.Invoke();
        GameSfxManager.Instance?.PlayShoot();
    }

    private void OnDestroy()
    {
        if (activeGun != null)
            activeGun.OnShotFired -= HandleShotFired;
    }

    public void EquipWeapon(GunProfile profile)
    {
        if (activeGun == null || profile == null)
            return;

        activeGun.ApplyProfile(profile);
    }

    public void ApplyUpgrade(GunUpgrade upgrade)
    {
        if (activeGun == null || upgrade == null)
            return;

        activeGun.ApplyUpgrade(upgrade);
    }

    public void SetStartingProfile(GunProfile profile)
    {
        startingProfile = profile;

        if (activeGun != null && profile != null)
            activeGun.ApplyProfile(profile);
    }

    public bool FiredThisFrame() => firedThisFrame;

    public Projectile GetProjectilePrefab() => activeGun != null ? activeGun.GetProjectilePrefab() : null;

    public Gun GetActiveGun() => activeGun;
}