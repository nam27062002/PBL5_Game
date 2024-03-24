using UnityEngine;
using UnityEngine.UI;

public class CameraManagerUI : MonoBehaviour
{
    private enum CameraStatus
    {
        None,
        DoNotHave,
        One,
        MoreThanOne
    }
    
    [SerializeField] private RawImage webcamImage;
    [SerializeField] private CameraStatus cameraStatus = CameraStatus.None;
    [SerializeField] private Image popupImage;
    [SerializeField] private Sprite haveCameraSprite;
    [SerializeField] private Sprite doNotCameraSprite;
    [SerializeField] private GameObject noCamera;
    private CameraStatus _lastStatusCamera = CameraStatus.None;
    private const int CameraDeviceIndex = 0;
    
    private void Update()
    {
        GetStatusCamera();
        ShowCameraHandler();
        SetBorderPopUp();
        ActiveNoCameraUI();
        SetLastStatusCamera();
    }


    private void GetStatusCamera()
    {
        cameraStatus = WebCamTexture.devices.Length switch
        {
            0 => CameraStatus.DoNotHave,
            1 => CameraStatus.One,
            > 1 => CameraStatus.MoreThanOne,
            _ => CameraStatus.None
        };
    }

    private void SetLastStatusCamera()
    {
        _lastStatusCamera = cameraStatus;
    }

    private void ShowCameraHandler()
    {
        
        switch (cameraStatus)
        {
            case CameraStatus.DoNotHave when _lastStatusCamera != CameraStatus.DoNotHave:
                Debug.LogError("Camera not found!");
                break;
            case CameraStatus.One when _lastStatusCamera != CameraStatus.One:
                ShowCameraDisplay();
                break;
            case CameraStatus.MoreThanOne when _lastStatusCamera != CameraStatus.MoreThanOne:
                ShowCameraDisplay();
                break;
            case CameraStatus.None:
            default:
                break;
        }
    }
    
    private void ShowCameraDisplay()
    {
        var deviceName = WebCamTexture.devices[CameraDeviceIndex].name;
        var webCamTexture = new WebCamTexture(deviceName);
        webcamImage.texture = webCamTexture;
        webCamTexture.Play();
    }

    private void SetBorderPopUp()
    {
        var sprite = cameraStatus == CameraStatus.DoNotHave ? doNotCameraSprite : haveCameraSprite;
        if (popupImage.sprite != sprite) popupImage.sprite = sprite;
    }

    private void ActiveNoCameraUI()
    {
        if (noCamera.activeSelf && cameraStatus != CameraStatus.DoNotHave) noCamera.SetActive(false);
        if (!noCamera.activeSelf && cameraStatus == CameraStatus.DoNotHave) noCamera.SetActive(true);
    }
}
