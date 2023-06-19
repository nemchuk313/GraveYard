using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GraveCreator : MonoBehaviour
{
    [SerializeField]
    private Button _buttonPrefab;

    [SerializeField]
    private Button _deadInfo;

    [SerializeField]
    private GameObject _deadInfoGo;

    [SerializeField]
    private List<GameObject> _graves = new List<GameObject>();

    [SerializeField]
    private GameObject _contentPanel;

    private Button _lastClickedButton;

    private void Start()
    {
        InitializeUI();
    }

    private void OnButtonClick(Button clickedButton)
    {
        _lastClickedButton = clickedButton;
    }

    private void Update()
    {
        CheckLastButtonClicked();
    }

    private void InitializeUI()
    {
        foreach (GameObject grave in _graves)
        {
            Button newButton = Instantiate(_buttonPrefab, _contentPanel.transform);
            newButton.image.sprite = grave.GetComponent<Grave>().graveSprite;

            newButton.onClick.AddListener(() => OnButtonClick(newButton));
        }
        // assign image for a dead portrait (Choose manualy)
        _deadInfo.image.sprite = DeadInfo.Instance._deadInfoScriptableObjects[0].portrait;
    }

    public void ZoomDeadInfo()
    {
        if(_deadInfoGo.activeSelf)
        {
            _deadInfoGo.SetActive(false);
        }
        else
        {
            _deadInfoGo.SetActive(true);
        }
    }

    private void CheckLastButtonClicked()
    {
        if (_lastClickedButton != null)
        {
            Debug.Log("Button Clicked: " + _lastClickedButton.name);

            _lastClickedButton = null;
        }
    }
}