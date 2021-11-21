using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;

using Newtonsoft.Json.Converters;
using System.Globalization;

using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Collections;

public static class CardDataManager
{

    #region Variable Declarations

    #region Card Set Data
    public static LinkedList<CardSetData> allCardSets;
    static LinkedList<string> setCodesOfDownloadedCardSets;

    #endregion

    #region File Path Constants

    const string cropArtFolder = "Crop";
    const string cardArtFolder = "Card";
    const string fileExtension = ".png";
    static string cardSetsDataFileName = Application.dataPath + Path.DirectorySeparatorChar + "Resources" + Path.DirectorySeparatorChar + "SetsAvailableForDownload.txt";

    //USED FOR INSTRUCTION
    //Before building, switch below used Application.datPath for Application.persistentDataPath as to avoid restricted folder locations.
    static string downloadedSetsByCodeFileName = Application.dataPath + Path.DirectorySeparatorChar + "Downloaded" + Path.DirectorySeparatorChar + "DownloadedSetsByCode.txt";
    static string cardSetsImagesFilePath = Application.dataPath + Path.DirectorySeparatorChar + "Downloaded" + Path.DirectorySeparatorChar + "CardSetImages" + Path.DirectorySeparatorChar;
    static string cardSetDataFilePath = Application.dataPath + Path.DirectorySeparatorChar + "Downloaded" + Path.DirectorySeparatorChar + "CardSetData" + Path.DirectorySeparatorChar;
    static string downloadFolder = Application.dataPath + Path.DirectorySeparatorChar + "Downloaded" + Path.DirectorySeparatorChar;

    #endregion

    #region Used To Download Card Images

    static LinkedList<CardSetData> downloadsInProgress;
    static float timeSinceLastDownloadRequest;
    static int downloadIndex;
    static GameObject threadHandlerGameObject;
    static CardData lastCardDownloaded;
    static LinkedList<CardSetData> verifyDownloadCompleted;
    static LinkedList<string> rotateThroughListBeforeReDownloading;
    static int threadLimiter;
    static int downloadingItemCompletionCount, downloadingItemTotalCount;
    static float timeToWaitBetweenThreadStarts = 0.125f;

    #endregion

    #endregion


    #region Setup & Update
    static public void Init()
    {
        allCardSets = new LinkedList<CardSetData>();
        downloadsInProgress = new LinkedList<CardSetData>();
        verifyDownloadCompleted = new LinkedList<CardSetData>();
        rotateThroughListBeforeReDownloading = new LinkedList<string>();

        #region Load Card Set Data File

        if (File.Exists(cardSetsDataFileName))
        {
            using (StreamReader sr = new StreamReader(cardSetsDataFileName))
            {
                string line = null;

                while ((line = sr.ReadLine()) != null)
                {
                    //Debug.Log("loading = " + line);

                    string[] procStr = line.Split(',');

                    CardSetData csd = new CardSetData();
                    csd.name = procStr[0];
                    csd.search_uri = procStr[1];
                    csd.block = procStr[2];
                    csd.code = procStr[3];
                    csd.set_type = procStr[4];
                    allCardSets.AddLast(csd);
                }
            }
        }

        #endregion

        EnsureDirectoryStructureExists();

        #region Load setCodesOfDownloadedCardSets

        setCodesOfDownloadedCardSets = new LinkedList<string>();

        if (File.Exists(downloadedSetsByCodeFileName))
        {
            using (StreamReader sr = new StreamReader(downloadedSetsByCodeFileName))
            {
                string line = null;

                while ((line = sr.ReadLine()) != null)
                {
                    setCodesOfDownloadedCardSets.AddLast(line);
                }
            }
        }
        else
        {
            //create file of downloaded sets
            using (StreamWriter sw = new StreamWriter(downloadedSetsByCodeFileName))
            {
                //INCLUDE PRELOADED SETS CODES HERE
                //sw.WriteLine("");
                //downloadedSetsByCode.AddLast();
            }
        }

        #endregion

        threadHandlerGameObject = new GameObject("ThreadHandler");
        threadHandlerGameObject.AddComponent<DummyCoroutineHandler>();

        // foreach (CardSetData csd in allSets)
        // {
        //     Debug.Log(csd.name);
        // }

    }

    static public void Update()
    {
        UpdateBulkImageDownloadProcess();
    }

    #endregion


    #region Download and Manage Card Set Data
    static public void SetupDownloadForCardSetContent(CardSetData cardSetData)
    {
        if (cardSetData != null)
        {
            if (cardSetData.cards == null)
            {
                Debug.Log("Downloading " + cardSetData.name);
                DownloadAndSaveCardSetData(cardSetData);
                System.IO.Directory.CreateDirectory(cardSetsImagesFilePath + Path.DirectorySeparatorChar + cardSetData.code);

                System.IO.Directory.CreateDirectory(cardSetsImagesFilePath + Path.DirectorySeparatorChar + cardSetData.code + Path.DirectorySeparatorChar + cardArtFolder);
                System.IO.Directory.CreateDirectory(cardSetsImagesFilePath + Path.DirectorySeparatorChar + cardSetData.code + Path.DirectorySeparatorChar + cropArtFolder);

                downloadsInProgress.AddLast(cardSetData);
                if (downloadsInProgress.Count == 1 && verifyDownloadCompleted.Count == 0)
                    SetupDownloadingItemCount();
            }
            else
                Debug.Log("Set has already been downloaded " + cardSetData.code);

        }
    }
    static private void DownloadAndSaveCardSetData(CardSetData cardSetData)// string link, string setNameCode)
    {
        LinkedList<CardData> cards = new LinkedList<CardData>();

        CardSetDataRootObject cardSetDataRootObject = null;
        string moreContentLink = "";

        bool hasMore = true;

        while (hasMore)
        {
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                string json = "";
                if (moreContentLink == "")
                    json = wc.DownloadString(cardSetData.search_uri);
                else
                    json = wc.DownloadString(moreContentLink);

                cardSetDataRootObject = JsonConvert.DeserializeObject<CardSetDataRootObject>(json);

                foreach (var s in cardSetDataRootObject.data)
                    cards.AddLast(s);

                if (cardSetDataRootObject.has_more)
                    moreContentLink = cardSetDataRootObject.next_page;
                else
                    hasMore = false;
            }
        }

        cardSetData.cards = cards;

        string cardSetJson = Newtonsoft.Json.JsonConvert.SerializeObject(cards);

        using (StreamWriter file = new StreamWriter(cardSetDataFilePath + cardSetData.name + ".txt"))
        {
            file.WriteLine(cardSetJson);
        }
    }
    static public void LoadCardSetFromDisk(CardSetData cardSetData)
    {
        string file = cardSetDataFilePath + cardSetData.name + ".txt";

        if (File.Exists(file))
        {
            cardSetData.cards = new LinkedList<CardData>();

            using (StreamReader sr = new StreamReader(file))
            {
                //string line = null;
                string jsonInfo = sr.ReadToEnd();

                foreach (CardData cd in Newtonsoft.Json.JsonConvert.DeserializeObject<List<CardData>>(jsonInfo))
                    cardSetData.cards.AddLast(cd);


                ///Placeholder*************************
                // while ((line = sr.ReadLine()) != null)
                // {
                //     CardData c = new CardData(line);
                //     Debug.Log("Loading from disk " + c.name);
                //     cardSetData.cards.AddLast(c);
                // }
            }
        }

    }
    static public CardSetData GetCardSetWithNameOrCode(string name, string code)
    {
        foreach (CardSetData csd in allCardSets)
        {
            if (csd.name == name || csd.code == code)
                return csd;
        }

        return null;
    }

    #endregion


    #region Image Downloading

    static private IEnumerator DownloadAndSaveImage(string url, string fileSaveName)
    {
        //Debug.Log("Downloading " + url + "  -  " + fileSaveName);

        //Debug.Log("active threads = " + threadLimiter);

        try
        {
            threadLimiter++;

            string filePath = cardSetsImagesFilePath + fileSaveName + fileExtension;
            Texture2D tex = null;

            // if (File.Exists(filePath))
            // {
            //     tex = LoadPNG(filePath);
            //     Debug.Log("loadinbg from file");
            // }
            // else
            //{

            //Debug.Log("starting request!");
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log("--Download Failure-");
                Debug.Log("url: " + url);
                Debug.Log("file: " + fileSaveName);
                Debug.Log(www.error);
                Debug.Log("-------");
            }
            else
            {
                //Debug.Log("saving downloaded image");
                tex = (Texture2D)((DownloadHandlerTexture)www.downloadHandler).texture;

                byte[] bytes;
                if (fileExtension == ".png")
                    bytes = tex.EncodeToPNG();
                else
                    bytes = tex.EncodeToJPG();

                File.WriteAllBytes(filePath, bytes);
                UpdateNumberOfItemsDownloaded();
            }
        }
        finally
        {
            threadLimiter--;
        }

    }

    static public void UpdateNumberOfItemsDownloaded()
    {
        bool skip = false;
        string path = cardSetsImagesFilePath;

        if (verifyDownloadCompleted.Count > 0)
            path = path + Path.DirectorySeparatorChar + verifyDownloadCompleted.First.Value.code;
        else if (downloadsInProgress.Count > 0)
            path = path + Path.DirectorySeparatorChar + downloadsInProgress.First.Value.code;
        else
        {
            downloadingItemCompletionCount = downloadingItemTotalCount;
            skip = true;
        }

        if (!skip)
            downloadingItemCompletionCount = Directory.GetFiles(path, "*" + fileExtension, SearchOption.AllDirectories).Length;
    }

    static public void UpdateBulkImageDownloadProcess()
    {
        if (threadLimiter < 6)
        {
            if (verifyDownloadCompleted.Count > 0)
            {
                if (timeSinceLastDownloadRequest < timeToWaitBetweenThreadStarts)
                    timeSinceLastDownloadRequest += Time.deltaTime;
                else
                {
                    timeSinceLastDownloadRequest = 0;

                    bool isClear = true;

                    foreach (CardSetData csd in verifyDownloadCompleted)
                    {
                        foreach (CardData cd in csd.cards)
                        {

                            string url = "", fileSaveName;

                            if (cd.image_uris == null)
                            {
                                Debug.Log("Skipping verification of card: " + cd.name + " due to null image_uris");
                            }
                            else
                            {

                                url = cd.image_uris.png;
                                fileSaveName = cardArtFolder + Path.DirectorySeparatorChar + cd.name;
                                fileSaveName = csd.code + Path.DirectorySeparatorChar + fileSaveName;
                                //fileSaveName = "/CardSetImages/" + csd.code + "/" + fileSaveName;

                                if (!File.Exists(cardSetsImagesFilePath + fileSaveName + fileExtension) && !rotateThroughListBeforeReDownloading.Contains(fileSaveName))
                                {
                                    Debug.Log("redownloading" + fileSaveName);
                                    isClear = false;
                                    rotateThroughListBeforeReDownloading.AddLast(fileSaveName);
                                    url = cd.image_uris.png;
                                    threadHandlerGameObject.GetComponent<DummyCoroutineHandler>().StartCoroutine(CardDataManager.DownloadAndSaveImage(url, fileSaveName));
                                    break;
                                }

                                url = cd.image_uris.art_crop;
                                fileSaveName = cropArtFolder + Path.DirectorySeparatorChar + cd.name;
                                fileSaveName = csd.code + Path.DirectorySeparatorChar + fileSaveName;
                                if (!File.Exists(cardSetsImagesFilePath + fileSaveName + fileExtension) && !rotateThroughListBeforeReDownloading.Contains(fileSaveName))
                                {
                                    Debug.Log("redownloading" + fileSaveName);
                                    isClear = false;
                                    rotateThroughListBeforeReDownloading.AddLast(fileSaveName);
                                    url = cd.image_uris.art_crop;
                                    threadHandlerGameObject.GetComponent<DummyCoroutineHandler>().StartCoroutine(CardDataManager.DownloadAndSaveImage(url, fileSaveName));
                                    break;
                                }
                            }
                        }
                        break;
                    }

                    if (isClear)
                    {
                        if (rotateThroughListBeforeReDownloading.Count > 0)
                        {
                            rotateThroughListBeforeReDownloading.Clear();
                            Debug.Log("Rotating through verification process");
                        }
                        else
                        {
                            Debug.Log("Verification Complete");
                            setCodesOfDownloadedCardSets.AddLast(verifyDownloadCompleted.First.Value.code);
                            File.AppendAllText(downloadedSetsByCodeFileName, "\n" + verifyDownloadCompleted.First.Value.code);
                            verifyDownloadCompleted.RemoveFirst();
                            if (downloadsInProgress.Count > 0)
                                SetupDownloadingItemCount();
                        }
                    }
                }
            }
            else if (downloadsInProgress.Count > 0)
            {
                if (timeSinceLastDownloadRequest < 0.25f)
                    timeSinceLastDownloadRequest += Time.deltaTime;
                else
                {
                    timeSinceLastDownloadRequest = 0;
                    CardSetData csdToUse = null;
                    CardData cdToUse = null;
                    bool useNext = false;

                    foreach (CardSetData csd in downloadsInProgress)
                    {
                        foreach (CardData cd in csd.cards)
                        {

                            if (lastCardDownloaded == null)
                            {
                                lastCardDownloaded = cd;
                                cdToUse = cd;
                                csdToUse = csd;
                                break;
                            }
                            else if (lastCardDownloaded == cd)
                            {
                                if (downloadIndex == 1)
                                    useNext = true;
                                else
                                {
                                    cdToUse = cd;
                                    csdToUse = csd;
                                }
                            }
                            else if (useNext)
                            {
                                useNext = false;
                                lastCardDownloaded = cd;
                                cdToUse = cd;
                                csdToUse = csd;
                            }

                        }

                        break;
                    }


                    if (useNext)
                    {
                        lastCardDownloaded = null;
                        verifyDownloadCompleted.AddLast(downloadsInProgress.First.Value);
                        downloadsInProgress.RemoveFirst();

                        //add setcode to downloaded sets list and append to file of list
                    }
                    else if (csdToUse != null && cdToUse != null)
                    {
                        string url = "", fileSaveName = "";


                        if (cdToUse.image_uris == null)
                        {
                            Debug.Log("Null image_uris found with card: " + cdToUse.name);
                        }
                        else if (downloadIndex == 0)
                        {
                            url = cdToUse.image_uris.png;
                            fileSaveName = cardArtFolder + Path.DirectorySeparatorChar + cdToUse.name;
                        }
                        else //if(downloadIndex == 1)
                        {
                            url = cdToUse.image_uris.art_crop;
                            fileSaveName = cropArtFolder + Path.DirectorySeparatorChar + cdToUse.name;
                        }

                        if (url != "")
                        {
                            fileSaveName = csdToUse.code + Path.DirectorySeparatorChar + fileSaveName;
                            //unityThreadHandler.StopAllCoroutines();
                            threadHandlerGameObject.GetComponent<DummyCoroutineHandler>().StartCoroutine(CardDataManager.DownloadAndSaveImage(url, fileSaveName));
                        }

                        downloadIndex++;
                        if (downloadIndex == 2)
                            downloadIndex = 0;

                    }
                }
            }
        }
    }

    #endregion


    #region UI Utilities
    static public Sprite LoadTextureFileIntoSprite(CardSetData cardSetData, CardData cardData, bool isCrop)
    {
        string prefix;
        if (isCrop)
            prefix = cropArtFolder;
        else
            prefix = cardArtFolder;

        string file = cardSetsImagesFilePath +
            Path.DirectorySeparatorChar + cardSetData.code +
            Path.DirectorySeparatorChar + prefix +
            Path.DirectorySeparatorChar + cardData.name
            + fileExtension;

        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(file))
        {
            fileData = File.ReadAllBytes(file);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        else
            Debug.Log("Unable to find file: " + file);

        if (tex != null)
        {
            Sprite newSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return newSprite;
        }
        else
            Debug.Log("Unable to create sprite " + cardData.name);

        return null;
    }

    static private void SetupDownloadingItemCount()
    {
        downloadingItemCompletionCount = 0;
        downloadingItemTotalCount = downloadsInProgress.First.Value.cards.Count * 2;
    }
    static public int GetDownloadingItemCompletionCount()
    {
        return downloadingItemCompletionCount;
    }
    static public int GetDownloadingItemTotalCount()
    {
        return downloadingItemTotalCount;
    }

    #endregion


    #region File Management Utilties
    static private void EnsureDirectoryStructureExists()
    {
        if (!File.Exists(downloadFolder))
            System.IO.Directory.CreateDirectory(downloadFolder);

        if (!File.Exists(cardSetsImagesFilePath))
            System.IO.Directory.CreateDirectory(cardSetsImagesFilePath);

        if (!File.Exists(cardSetDataFilePath))
            System.IO.Directory.CreateDirectory(cardSetDataFilePath);
    }

    static public void DeleteAllDownloadedData()
    {
        Directory.Delete(downloadFolder, true);
        EnsureDirectoryStructureExists();
    }

    #endregion

}

public class DummyCoroutineHandler : MonoBehaviour
{
    //TODO: something something
    //TAG: use to note somethign that must be updated 
    //FIXME: noted bug
}

