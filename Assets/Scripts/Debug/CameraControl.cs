using System.Collections;
using UnityEngine;

public class CameraControl : MonoBehaviour
{

    public Transform orthoTransform;

    public float orthoSize = 38f;

    public Transform freeCamTransform;
    public float cameraSpeed = 3f;
    public float sensitivity = 0.5f;

    public void SetOrtho(bool val)
    {
        if (val)
        {
            Camera.main.orthographic = true;
            Camera.main.orthographicSize = orthoSize;
            Camera.main.transform.SetParent(orthoTransform);
            Camera.main.transform.localPosition = Vector3.zero;
            Camera.main.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Camera.main.orthographic = false;
            Camera.main.transform.SetParent(freeCamTransform);
            Camera.main.transform.localPosition = Vector3.zero;
            Camera.main.transform.localRotation = Quaternion.identity;
        }
    }

    public void ToggleOrtho()
    {
        SetOrtho(!Camera.main.orthographic);
    }

    private void Update()
    {
        //update input on the freecam
        if (!Camera.main.orthographic)
        {
            //toggle mouse
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Cursor.visible = !Cursor.visible;
                Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
            }

            if (!Cursor.visible)
            {
                Transform cameraTransform = Camera.main.transform;
                Vector3 inputVec = Vector3.zero;

                if (Input.GetKey(KeyCode.W))
                {
                    inputVec += cameraTransform.forward;
                }
                if (Input.GetKey(KeyCode.S))
                {
                    inputVec -= cameraTransform.forward;
                }
                if (Input.GetKey(KeyCode.D))
                {
                    inputVec += cameraTransform.right;
                }
                if (Input.GetKey(KeyCode.A))
                {
                    inputVec -= cameraTransform.right;
                }
                if (Input.GetKey(KeyCode.E))
                {
                    inputVec += cameraTransform.up;
                }
                if (Input.GetKey(KeyCode.Q))
                {
                    inputVec -= cameraTransform.up;
                }

                //now that we have the input vec, normalize and multiply by camera speed
                Vector3 movementVec = Vector3.Normalize(inputVec) * cameraSpeed * Time.deltaTime;
                freeCamTransform.position += movementVec;

                //handle rotation
                float newY = freeCamTransform.rotation.eulerAngles.y + Input.GetAxis("Mouse X") * sensitivity;
                float newX = freeCamTransform.rotation.eulerAngles.x - Input.GetAxis("Mouse Y") * sensitivity;

                //clamp based on wheter player is looking up or down
                if (newX > 180f)
                {
                    newX = Mathf.Clamp(newX, 360f - 89.99f, 370f);
                }
                else
                {
                    newX = Mathf.Clamp(newX, -10f, 89.99f);
                }

                freeCamTransform.rotation = Quaternion.Euler(newX, newY, 0f);
            }
        }
    }
}