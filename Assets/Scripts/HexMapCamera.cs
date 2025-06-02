using System;
using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    [SerializeField]
    private Transform swivel, stick;
    [SerializeField]
    private float stickMinZoom = -250, stickMaxZoom = -45;
    [SerializeField]
    private float swivelMinZoom = 90, swivelMaxZoom = 45;
    [SerializeField]
    private float moveSpeedMinZoom = 400, moveSpeedMaxZoom = 100;
    [SerializeField]
    private float rotationSpeed = 180;
    [SerializeField]
    private HexGrid grid;

    float zoom = 1f;
    float rotationAngle;

    void Awake()
    {
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta);
        }

        float rotationDelta = Input.GetAxis("Rotation");
        if (rotationDelta != 0f)
        {
            AdjustRotation(rotationDelta);
        }

        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");
        if (xDelta != 0f || zDelta != 0f)
        {
            AdjustPosition(xDelta, zDelta);
        }
    }

    void AdjustZoom(float delta)
    {
        //好难，先知道可以这样做吧
        zoom = Mathf.Clamp01(zoom + delta);

        //调整相机的位置
        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0f, 0f, distance);

        //调整相机的角度
        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    void AdjustRotation(float delta)
    {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;
        if (rotationAngle < 0f)
        {
            rotationAngle += 360f;
        }
        else if (rotationAngle >= 360f)
        {
            rotationAngle -= 360f;
        }

        transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
        // Debug.Log(transform.localRotation);
    }

    //https://catlikecoding.com/unity/tutorials/hex-map/part-5/#2.2
    //具体思考过程看这里
    public void AdjustPosition(float xDelta, float zDelta)
    {
        //归一化的含义我还不是很清楚，规范化之后的数值就是[-1,1]之间，可以当作方向来使用了
        Vector3 direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
        float distance = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) *
              damping * Time.deltaTime;

        //Debug.Log("direction:" + direction);
        //Debug.Log("damping:" + damping);

        Vector3 position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = ClampPosition(position);
    }

    Vector3 ClampPosition(Vector3 position)
    {
        float xMax = (grid.CellCountX - 0.5f) * (2f * HexMetrics.innerRadius);
        position.x = Mathf.Clamp(position.x, 0f, xMax);

        float zMax = (grid.CellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z, 0, zMax);
        return position;
    }
}