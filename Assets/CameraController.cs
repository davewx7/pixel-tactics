using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    UnityEngine.U2D.PixelPerfectCamera _pixelPerfectCamera = null;

    [SerializeField]
    GameController _controller;

    [SerializeField]
    float _speed = 1f;

    [SerializeField]
    float _edgePan = 2f;

    bool useEdgePan {
        get {
            return Screen.fullScreen;
        }
    }

    float horizontal {
        get {
            if(Application.isFocused == false || GameController.instance.uiHasFocus) {
                return 0f;
            }

            float result = Input.GetAxis("Horizontal");
            if(Application.isEditor) {
                return result;
            }

            if(useEdgePan) {
                if(Input.mousePosition.x < _edgePan) {
                    result = -1f;
                } else if(Input.mousePosition.x > Screen.width - _edgePan) {
                    result = 1f;
                }
            }

            return result;
        }
    }

    float vertical {
        get {
            if(Application.isFocused == false || GameController.instance.uiHasFocus) {
                return 0f;
            }

            float result = Input.GetAxis("Vertical");
            if(Application.isEditor) {
                return result;
            }

            if(useEdgePan) {
                if(Input.mousePosition.y < _edgePan) {
                    result = -1f;
                } else if(Input.mousePosition.y > Screen.height - _edgePan) {
                    result = 1f;
                }
            }

            return result;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(GameConfig.modalDialog == 0) {
            transform.position += new Vector3(horizontal, vertical, 0f)*_speed*Time.deltaTime;
        }

        int xres = (_pixelPerfectCamera.refResolutionY*Screen.width)/Screen.height;
        if(xres%2 == 1) {
            --xres;
        }

        _pixelPerfectCamera.refResolutionX = xres;
    }
}
