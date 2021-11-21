using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System.Threading;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SampleUI : MonoBehaviour
{
    public GameObject textDownloadStatus;
    public GameObject inputSetCode, inputCardName;
    public GameObject cardSpriteDisplay, cardInfoDisplay;
    public GameObject buttonDownload, buttonLoadFromHD, buttonUnload, buttonLoadImage;

    void Start()
    {
        buttonDownload.GetComponent<Button>().onClick.AddListener(ButtonDownloadSetClicked);
        buttonLoadFromHD.GetComponent<Button>().onClick.AddListener(ButtonLoadSetFromDisk);
        buttonUnload.GetComponent<Button>().onClick.AddListener(ButtonUnloadSet);
        buttonLoadImage.GetComponent<Button>().onClick.AddListener(ButtonLoadCardImageAndInfoFromSet);
    }
    void Update()
    {
        textDownloadStatus.GetComponent<Text>().text = CardDataManager.GetDownloadingItemCompletionCount() + "/" + CardDataManager.GetDownloadingItemTotalCount();
    }
    public void ButtonDownloadSetClicked()
    {
        string setCodeParam = inputSetCode.GetComponent<InputField>().text;
        CardSetData setData = CardDataManager.GetCardSetWithNameOrCode("", setCodeParam);

        if (setData != null)
            CardDataManager.SetupDownloadForCardSetContent(setData);
    }
    public void ButtonUnloadSet()
    {
        string setCodeParam = inputSetCode.GetComponent<InputField>().text;
        CardSetData setData = CardDataManager.GetCardSetWithNameOrCode("", setCodeParam);

        if (setData != null)
            setData.cards = null;

        //For testing
        //SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
    public void ButtonLoadSetFromDisk()
    {
        string setCodeParam = inputSetCode.GetComponent<InputField>().text;
        CardSetData setData = CardDataManager.GetCardSetWithNameOrCode("", setCodeParam);

        if (setData != null)
            CardDataManager.LoadCardSetFromDisk(setData);
    }
    public void ButtonLoadCardImageAndInfoFromSet()
    {
        string setCodeParam = inputSetCode.GetComponent<InputField>().text;
        CardSetData setData = CardDataManager.GetCardSetWithNameOrCode("", setCodeParam);

        if (setData != null)
        {
            string cardNameParam = inputCardName.GetComponent<InputField>().text;

            if (setData.cards != null)
            {
                foreach (CardData card in setData.cards)
                {
                    if (card.name == cardNameParam)
                    {
                        cardSpriteDisplay.GetComponent<SpriteRenderer>().sprite = CardDataManager.LoadTextureFileIntoSprite(setData, card, false);
                        
                        string ci = card.name + "\n";
                        ci = ci + "Colors: ";
                        foreach(string c in card.color_identity)
                            ci = ci + c;
                        ci = ci + "\n";
                        ci = ci + "Types: " + card.type_line + "\n";
                        ci = ci + card.oracle_text + "\n";
                        ci = ci + "P/T: " + card.power + "/" + card.toughness + "\n";
                        ci = ci + "Loyalty: " + card.loyalty + "\n";
                        cardInfoDisplay.GetComponent<Text>().text = ci;
                        break;
                    }
                }
            }
            else
                Debug.Log("Cards not loaded for set: " + setData.name);
        }
    }

}
