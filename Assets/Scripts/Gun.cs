using System;
using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public enum State
    {
        Ready,
        Empty,
        Reloading
    }
    public State state { get; private set; }
    
    private PlayerShooter gunHolder;
    private LineRenderer bulletLineRenderer;
    
    private AudioSource gunAudioPlayer;
    public AudioClip shotClip;
    public AudioClip reloadClip;
    
    public ParticleSystem muzzleFlashEffect;
    public ParticleSystem shellEjectEffect;
    
    public Transform fireTransform;
    public Transform leftHandMount;

    public float damage = 25;
    public float fireDistance = 100f;

    public int ammoRemain = 100;
    public int magAmmo;
    public int magCapacity = 30;

    public float timeBetFire = 0.12f;
    public float reloadTime = 1.8f;
    
    [Range(0f, 10f)] public float maxSpread = 3f;
    [Range(1f, 10f)] public float stability = 1f;
    [Range(0.01f, 3f)] public float restoreFromRecoilSpeed = 2f;
    private float currentSpread;
    private float currentSpreadVelocity;

    private float lastFireTime;

    private LayerMask excludeTarget;

    private void Awake()
    {
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        bulletLineRenderer.positionCount = 2;
        bulletLineRenderer.enabled = false;
    }

    public void Setup(PlayerShooter gunHolder)
    {
        this.gunHolder = gunHolder;
        excludeTarget = gunHolder.excludeTarget;
    }

    private void OnEnable()
    {
        magAmmo = magCapacity;
        currentSpread = 0f;
        lastFireTime = 0f;
        state = State.Ready;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public bool Fire(Vector3 aimTarget)
    {
        if(state == State.Ready && Time.time >= lastFireTime + timeBetFire)
        {
            var fireDirectrion = aimTarget - fireTransform.position; //이러면 에임핵 마냥 정확하게 박힘

            //정규 분포를 사용해서 탄퍼짐을 구현
            var xError = Utility.GedRandomNormalDistribution(0f, currentSpread);
            var yError = Utility.GedRandomNormalDistribution(0f, currentSpread);

            fireDirectrion = Quaternion.AngleAxis(yError, Vector3.up) * fireDirectrion;
            fireDirectrion = Quaternion.AngleAxis(xError, Vector3.right) * fireDirectrion;

            currentSpread += 1f / stability; //총기 안정성에 따라 탄퍼짐이 증가하는 것을 구현
            //정규 분포를 사용해서 탄퍼짐을 구현

            lastFireTime = Time.time;
            Shot(fireTransform.position, fireDirectrion);

            return true;
        }
        return false;
    }
    
    private void Shot(Vector3 startPoint, Vector3 direction)
    {
        RaycastHit hit;
        Vector3 hitPosition;

        if(Physics.Raycast(startPoint, direction, out hit, fireDistance, ~excludeTarget))
        {
            var target = hit.collider.GetComponent<IDamageable>(); //데미지를 줄수있는 타겟인가?
            if( target != null)
            {
                DamageMessage damageMessage;

                damageMessage.damager = gunHolder.gameObject;
                damageMessage.amount = damage;
                damageMessage.hitPoint = hit.point;
                damageMessage.hitNormal = hit.normal;

                target.ApplyDamage(damageMessage);
            }
            hitPosition = hit.point;
        }
        else
        {
            hitPosition = startPoint + direction * fireDistance;
        }

        StartCoroutine(ShotEffect(hitPosition));

        magAmmo--;
        if (magAmmo <= 0)
            state = State.Empty;
    }

    private IEnumerator ShotEffect(Vector3 hitPosition) //코루틴을 사용해 대기시간을 주기 위함
    {
        muzzleFlashEffect.Play();
        shellEjectEffect.Play();

        gunAudioPlayer.PlayOneShot(shotClip); //중복으로 소리가 남

        bulletLineRenderer.enabled = true;
        bulletLineRenderer.SetPosition(0, fireTransform.position);
        bulletLineRenderer.SetPosition(1, hitPosition);

        yield return new WaitForSeconds(0.03f);

        bulletLineRenderer.enabled = false;
    }
    
    public bool Reload()
    {
        if(state == State.Reloading || ammoRemain<=0 || magAmmo >= magCapacity)
        {
            return false;
        }

        StartCoroutine(ReloadRoutine());

        return true;
    }

    private IEnumerator ReloadRoutine()
    {
        state = State.Reloading;
        gunAudioPlayer.PlayOneShot(reloadClip);

        yield return new WaitForSeconds(reloadTime);

        var ammoToFill = Mathf.Clamp(magCapacity - magAmmo, 0, ammoRemain);

        magAmmo += ammoToFill;
        ammoRemain -= ammoToFill;

        state = State.Ready;
    }

    private void Update()
    {
        //가만히 있는다면 탄퍼짐을 감소시켜야함
        currentSpread = Mathf.Clamp(currentSpread, 0f, maxSpread);
        currentSpread = Mathf.SmoothDamp(currentSpread, 0f, ref currentSpreadVelocity, 1f / restoreFromRecoilSpeed);

    }
}