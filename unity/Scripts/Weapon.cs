using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] Camera FPCamera;
    [SerializeField] float range = 100f;
    [SerializeField] float damage = 30f;
    [SerializeField] ParticleSystem muzzleFlash;
    [SerializeField] GameObject hitEffect;
    [SerializeField] Ammo ammoSlot;
    [SerializeField] AmmoType ammoType;
    [SerializeField] float timeBetweenShots = 0.5f;
    [SerializeField] TextMeshProUGUI ammoText;

    bool canShoot = true;
    string serialFilePath;

    private void OnEnable()
    {
        canShoot = true;
        serialFilePath = Application.dataPath + "/serial_output.txt";
    }

    void Update()
    {
        DisplayAmmo();

        // Read from file
        string command = ReadSerialCommand();

        if (command == "Shoot" && canShoot)
        {
            StartCoroutine(Shoot());
        }
    }

    private string ReadSerialCommand()
    {
        try
        {
            if (File.Exists(serialFilePath))
            {
                string content = File.ReadAllText(serialFilePath).Trim();

                if (content == "Shoot")
                {
                    // Optional: Clear after reading to avoid repeating shots
                    File.WriteAllText(serialFilePath, "");
                    return "Shoot";
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Error reading serial command: " + e.Message);
        }

        return "";
    }

    private void DisplayAmmo()
    {
        int currentAmmo = ammoSlot.GetCurrentAmmo(ammoType);
        ammoText.text = currentAmmo.ToString();
    }

    IEnumerator Shoot()
    {
        canShoot = false;
        if (ammoSlot.GetCurrentAmmo(ammoType) > 0)
        {
            PlayMuzzleFlash();
            ProcessRaycast();
            ammoSlot.ReduceCurrentAmmo(ammoType);
        }
        yield return new WaitForSeconds(timeBetweenShots);
        canShoot = true;
    }

    private void PlayMuzzleFlash()
    {
        muzzleFlash.Play();
    }

    private void ProcessRaycast()
    {
        RaycastHit hit;
        if (Physics.Raycast(FPCamera.transform.position, FPCamera.transform.forward, out hit, range))
        {
            CreateHitImpact(hit);
            EnemyHealth target = hit.transform.GetComponent<EnemyHealth>();
            if (target == null) return;
            target.TakeDamage(damage);
        }
    }

    private void CreateHitImpact(RaycastHit hit)
    {
        GameObject impact = Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
        Destroy(impact, .1f);
    }
}