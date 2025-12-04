using UnityEngine;

public class WingardiumLeviosa : MonoBehaviour
{
    public float liftHeight = 2f;       // how high to lift
    public float liftSpeed = 2f;        // how fast to lift
    public float holdTime = 3f;         // how long to hold before dropping

    private float timer = 0f;
    private Transform target;

    void Start()
    {
        // Raycast forward from camera to find target
        Transform cam = Camera.main.transform;
        Ray ray = new Ray(cam.position, cam.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 10f))
        {
            if (hit.collider.CompareTag("Weapon") || hit.collider.CompareTag("Enemy"))
            {
                target = hit.collider.transform;
            }
        }
    }

    void Update()
    {
        if (target == null) return;

        timer += Time.deltaTime;

        if (timer <= holdTime)
        {
            // Smoothly move target upward
            Vector3 targetPos = target.position;
            targetPos.y = Mathf.Lerp(target.position.y, target.position.y + liftHeight, Time.deltaTime * liftSpeed);
            target.position = targetPos;
        }
        else
        {
            // Release after hold time
            Destroy(gameObject);
        }
    }
}