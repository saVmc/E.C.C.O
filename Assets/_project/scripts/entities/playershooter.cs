using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
public sealed class PlayerShooter : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireCooldown = 0.2f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLifetime = 2f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private Color projectileTintColor = Color.white;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private float muzzleDuration = 0.08f;
    [SerializeField] private bool allowVerticalShooting = false;

    private enum SpriteFacing { Right, Up }
    [SerializeField] private SpriteFacing projectileSpriteFacing = SpriteFacing.Right;

    private PlayerMovement playerMovement;
    private float nextFireTime;
    private bool firedThisFrame = false;

    public event Action OnShotFired;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();

        if (firePoint == null)
        {
            firePoint = transform;
        }
        if (muzzleFlash != null)
            muzzleFlash.SetActive(false);
    }

    private void Update()
    {
        firedThisFrame = false;

        if (!CanFire())
        {
            return;
        }

        bool firePressed = false;

        if (Keyboard.current != null)
        {
            firePressed |= Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        if (Mouse.current != null)
        {
            firePressed |= Mouse.current.leftButton.wasPressedThisFrame;
        }

        if (firePressed)
        {
            Fire();
        }
    }

    private bool CanFire()
    {
        return projectilePrefab != null && Time.time >= nextFireTime;
    }

    private void Fire()
    {
        firedThisFrame = true;
        Vector2 direction = playerMovement.GetFacingDirection();

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.right;
        }

        if (!allowVerticalShooting)
        {
            direction.y = 0f;
            if (Mathf.Approximately(direction.x, 0f))
                direction.x = 1f;
        }

        if (firePoint != null)
        {
            firePoint.localRotation = Quaternion.Euler(0, 0, (direction.x < 0f) ? 180f : 0f);
            
            Vector3 fpLocalPos = firePoint.localPosition;
            fpLocalPos.x = Mathf.Abs(firePoint.localPosition.x);
            if (direction.x < 0f)
                fpLocalPos.x = -fpLocalPos.x;
            firePoint.localPosition = fpLocalPos;
        }

        Projectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        projectile.Initialize(direction, projectileSpeed, projectileLifetime, projectileDamage, gameObject);

        SpriteRenderer projectileSprite = projectile.GetComponent<SpriteRenderer>();
        if (projectileSprite != null)
        {
            projectileSprite.color = projectileTintColor;
        }

        if (projectile != null)
        {
            projectile.transform.rotation = Quaternion.identity;
            if (projectileSpriteFacing == SpriteFacing.Right)
                projectile.transform.right = direction;
            else
                projectile.transform.up = direction;
        }

        nextFireTime = Time.time + fireCooldown;
        OnShotFired?.Invoke();
        if (muzzleFlash != null)
        {
            if (muzzleFlash.transform.parent == firePoint)
            {
                muzzleFlash.transform.localPosition = Vector3.zero;
                muzzleFlash.transform.localRotation = Quaternion.identity; 
            }
            else
            {
                muzzleFlash.transform.position = firePoint.position;
                if (projectileSpriteFacing == SpriteFacing.Right)
                    muzzleFlash.transform.right = direction;
                else
                    muzzleFlash.transform.up = direction;
            }
            StartCoroutine(MuzzleFlashCoroutine());
        }
    }

    private System.Collections.IEnumerator MuzzleFlashCoroutine()
    {
        muzzleFlash.SetActive(true);
        yield return new WaitForSeconds(muzzleDuration);
        if (muzzleFlash != null)
            muzzleFlash.SetActive(false);
    }

    public Color GetProjectileTintColor()
    {
        return projectileTintColor;
    }


    public bool FiredThisFrame() => firedThisFrame;

    public Projectile GetProjectilePrefab() => projectilePrefab;
}