using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;
using TMPro;
using System.Linq;
using UnityEditor.PackageManager;
using System;
using Unity.VisualScripting;

public class CratesManager : MonoBehaviour
{
    List<GameObject> crates = new List<GameObject>();
    public GameObject[] cratePrefabs;
    public GameObject[] SpawnLocations;
    public GameObject mergeMessageWindow;
    public GameObject mergeConfirmWindow;
    public GameObject crateBurningVFX;
    public TMP_Dropdown UI_DBSelector;
    public float transitionSpeed = 2f;
    bool buttonsUnlocked;
    int[] cratesToMergeIDs = new int[2]{-1,-1};
    string[] cratesToMergeTypes = new string[2];
    int currentCrateID = -1;

    //Dummy crates Data -> Feed with DB
    string dummyCratesDataJSON;
    //Dummy crates class structure for data handle
    CratesData dummyCratesData;

    //Data Selector in inspector
        public enum DUMMY_DATABASE_SELECTOR{
        DATABASE1,
        DATABASE2,
        DATABASE3
    }
    [field: SerializeField]
    public DUMMY_DATABASE_SELECTOR dummyDatabaseSelector {get; set ;}
    private DUMMY_DATABASE_SELECTOR currentdummyDatabaseSelector;
    
    void Start()
    {
        currentdummyDatabaseSelector = dummyDatabaseSelector;

        HandleDummyData();
        ResetCrateSystem();
        SpawnCrates();       
    }
    void Update()
    {
        if(crates.Count > 0)
        crates[0].transform.position = Vector3.Lerp(
            crates[0].transform.position, SpawnLocations[0].transform.position, Time.deltaTime * transitionSpeed);

        if(crates.Count > 1)
        crates[1].transform.position = Vector3.Lerp(
            crates[1].transform.position, SpawnLocations[1].transform.position, Time.deltaTime * transitionSpeed);

        if(crates.Count > 2)
            crates[2].transform.position = Vector3.Lerp(
                crates[2].transform.position, SpawnLocations[2].transform.position, Time.deltaTime * transitionSpeed);
        
        //Handle the DB Selector Change in Inspector
        if(currentdummyDatabaseSelector != dummyDatabaseSelector){
            currentdummyDatabaseSelector = dummyDatabaseSelector;
            HandleDummyData();
            ResetCrateSystem();
            SpawnCrates();
        }
    }

    void HandleDummyData(){
        if(dummyDatabaseSelector == DUMMY_DATABASE_SELECTOR.DATABASE1)
            dummyCratesDataJSON = "{\"data\": {" + 
                "\"crates\": [" + 
                    "{\"uid\": \"008\", \"type\": \"Common\", \"status\": \"Closed\"}," +
                    "{\"uid\": \"010\", \"type\": \"Uncommon\", \"status\": \"Closed\"}," +
                    "{\"uid\": \"020\", \"type\": \"Rare\", \"status\": \"Closed\"}," +
                    "{\"uid\": \"005\", \"type\": \"Epic\", \"status\": \"Closed\"}," +
                    "{\"uid\": \"012\", \"type\": \"Legendary\", \"status\": \"Closed\"}," +
                    "{\"uid\": \"046\", \"type\": \"Common\", \"status\": \"Closed\"}," +
                    "{\"uid\": \"087\", \"type\": \"Common\", \"status\": \"Closed\"}]}}";
        
        else if(dummyDatabaseSelector == DUMMY_DATABASE_SELECTOR.DATABASE2)
            dummyCratesDataJSON = "{\"data\": { \"crates\": [{\"uid\": \"001\", \"type\": \"Common\", \"status\": \"Closed\"}]}}";
        
        else if(dummyDatabaseSelector == DUMMY_DATABASE_SELECTOR.DATABASE3)
            dummyCratesDataJSON = "{\"data\": { \"crates\": []}}";
   
        dummyCratesData = JsonUtility.FromJson<CratesData>(dummyCratesDataJSON);

        //If length is less then 3, adds empty crate placeholders

        if(dummyCratesData.data.crates.Count < 3){
            int InitialLength = dummyCratesData.data.crates.Count;
            int maxUID = dummyCratesData.data.crates.Count > 0 ? dummyCratesData.data.crates.Max(crate => crate.uid) : 0;
            Debug.Log($"Initial Length: {InitialLength}, Max UID: {maxUID}");

            for(int i = 3; i - InitialLength > 0; i--){
                dummyCratesData.data.crates.Add(new Crate(maxUID + i, "Empty", "Closed"));
            }
        }
    }
    void ResetCrateSystem()
    {
        // Stop any running coroutines (VERY important)
        StopAllCoroutines();

        // Destroy all existing crates safely
        foreach (GameObject crate in crates)
        {
            if (crate != null)
                Destroy(crate);
        }
        crates.Clear();

        // Reset merge state
        ResetMergeCrates();

        // Reset UI
        mergeMessageWindow.SetActive(false);
        mergeConfirmWindow.SetActive(false);

        // Reset control
        currentCrateID = dummyCratesData.data.crates[0].uid;
        Debug.Log($"Current Crate ID reset to: {currentCrateID}");
        buttonsUnlocked = true;
    }
    void ResetMergeCrates(){
        cratesToMergeIDs[0] = -1;
        cratesToMergeIDs[1] = -1;
        cratesToMergeTypes[0] = "";
        cratesToMergeTypes[1] = "";
    }

    GameObject GetCratePrefabByType(string _type){
        foreach(GameObject cratePrefab in cratePrefabs){
            if(cratePrefab.tag == _type){
                return cratePrefab;
            }
        }
        return cratePrefabs[0];
    }
    void SpawnCrates(){
        Crate currentCrateData = GetCrateDataByUID(currentCrateID);
        Crate previousCrateData = GetCrateDataByUID(GetPreviousCrateUID());
        Crate nextCrateData = GetCrateDataByUID(GetNextCrateUID());

        crates.Add( GameObject.Instantiate(GetCratePrefabByType(previousCrateData.type)));
        crates.Add( GameObject.Instantiate(GetCratePrefabByType(currentCrateData.type))); 
        crates.Add( GameObject.Instantiate(GetCratePrefabByType(nextCrateData.type))); 
        
        crates[0].transform.position = SpawnLocations[0].transform.position;
        crates[0].transform.SetParent(transform);
        crates[0].name = $"Crate_{GetPreviousCrateUID()}";
        
        crates[1].transform.position = SpawnLocations[1].transform.position;
        crates[1].transform.SetParent(transform);
        crates[1].name = $"Crate_{currentCrateID}";
        
        crates[2].transform.position = SpawnLocations[2].transform.position;
        crates[2].transform.SetParent(transform);
        crates[2].name = $"Crate_{GetNextCrateUID()}";
        
        RefreshHighlights();
    }
    public void OnSwitchDummyData(){
        int index = UI_DBSelector.value;
        dummyDatabaseSelector = (DUMMY_DATABASE_SELECTOR)index;
    }
    public void OpenButton(){
        ResetMergeCrates();
        RefreshHighlights();
        if(crates[1].tag != "Empty")
            StartCoroutine(OpenAction());
    }

    IEnumerator OpenAction(){
        buttonsUnlocked = false;
        crates[1].GetComponent<Animator>().SetBool("Open", true);
        yield return new WaitForSeconds(8f);
        buttonsUnlocked = true;
        GetCrateDataByUID(currentCrateID).status = "Opened";
        yield return null;
    }
    
    public void GoToNextCrate(){
        if(buttonsUnlocked){
            buttonsUnlocked = false;
            StartCoroutine(SpawnNextCrate());
        }
    }

    public void GoToPreviousCrate(){
        if(buttonsUnlocked){
            buttonsUnlocked = false;
            StartCoroutine(SpawnPreviousCrate());
        }
    }
    int GetNextCrateUID(){
        int index = dummyCratesData.data.crates.FindIndex(crate => crate.uid == currentCrateID);
        int count = dummyCratesData.data.crates.Count;
        int modID = (index + 1) % count;
        int nextID = dummyCratesData.data.crates[modID].uid;
        return nextID;
    }
    int GetPreviousCrateUID(){
        int index = dummyCratesData.data.crates.FindIndex(crate => crate.uid == currentCrateID);
        int count = dummyCratesData.data.crates.Count;
        int modID = ((index - 1) % count + count) % count;
        int previousID = dummyCratesData.data.crates[modID].uid;
        return previousID;
    }
    int GetCrateIndexByUID(int uid){
        int index = dummyCratesData.data.crates.FindIndex(crate => crate.uid == uid);
        return index;
    }
    Crate GetCrateDataByUID(int uid){
        return dummyCratesData.data.crates.Find(crate => crate.uid == uid);
    }

    IEnumerator SpawnNextCrate(){
        currentCrateID = GetNextCrateUID();

        GameObject previousCrate = crates[0];
        previousCrate.GetComponent<Animator>().SetBool("Out", true);

        yield return new WaitForSeconds(0.25f);
        crates.RemoveAt(0);
        Crate nextCrateData = GetCrateDataByUID(GetNextCrateUID());
        GameObject newCrate = GameObject.Instantiate(GetCratePrefabByType(nextCrateData.type));
        newCrate.transform.position = SpawnLocations[2].transform.position;
        newCrate.transform.SetParent(transform);
        newCrate.name = $"Crate_{GetNextCrateUID()}";

        if(nextCrateData.status == "Opened"){
            newCrate.GetComponent<Animator>().SetBool("Open", true);
            newCrate.GetComponent<Animator>().Play("ItemFloating", 0, 0.0f);
        }
        
        crates.Insert(2, newCrate);
        yield return new WaitForSeconds(1/transitionSpeed);
        GameObject.Destroy(previousCrate);

        yield return new WaitForSeconds(0.25f);
        buttonsUnlocked = true;
        RefreshHighlights();

        yield return null;
    }

    IEnumerator SpawnPreviousCrate(){
        currentCrateID = GetPreviousCrateUID();

        crates[2].GetComponent<Animator>().SetBool("Out", true);
        
        yield return new WaitForSeconds(0.25f);
        Crate previousCrateData = GetCrateDataByUID(GetPreviousCrateUID());
        GameObject newCrate = GameObject.Instantiate(GetCratePrefabByType(previousCrateData.type));
        newCrate.transform.position = SpawnLocations[0].transform.position;
        newCrate.transform.SetParent(transform);
        newCrate.name = $"Crate_{GetPreviousCrateUID()}";

        if(previousCrateData.status == "Opened"){
            newCrate.GetComponent<Animator>().SetBool("Open", true);
            newCrate.GetComponent<Animator>().Play("ItemFloating", 0, 0.0f);
        }

        crates.Insert(0, newCrate);

        yield return new WaitForSeconds(2/transitionSpeed);
        GameObject.Destroy(crates[3]);
        crates.RemoveAt(3);

        yield return new WaitForSeconds(0.25f);
        buttonsUnlocked = true;
        RefreshHighlights();

        yield return null;
    }

    public void MergeCrates()
    {
        if(buttonsUnlocked)
        {
            if(crates[1].tag == "Empty" || GetCrateDataByUID(currentCrateID).status == "Opened")
                return;

             if(crates[1].tag == "Legendary"){
                mergeMessageWindow.transform.Find("Text").GetComponent<Text>().text = "This crate is already the highest tier.";
                mergeMessageWindow.SetActive(true);
                buttonsUnlocked = false;
                ResetMergeCrates();
                RefreshHighlights();
                return;
             }

            //Select the first Crate to Merge
            if(cratesToMergeIDs[0] == -1){
                cratesToMergeIDs[0] = currentCrateID;
                cratesToMergeTypes[0] = GetCrateDataByUID(currentCrateID).type;
                Debug.Log($"First Crate Selected with ID: {cratesToMergeIDs[0]} and type: {cratesToMergeTypes[0]}");
                RefreshHighlights();
                return;
            }
            //Check if user's trying to merge the same crate with itself
            if(currentCrateID == cratesToMergeIDs[0]){
                mergeMessageWindow.transform.Find("Text").GetComponent<Text>().text = "You can't merge the same crate.";
                mergeMessageWindow.SetActive(true);
                buttonsUnlocked = false;
                ResetMergeCrates();
                RefreshHighlights();
                return;
            }
            //Check if user's trying of different types
            if(cratesToMergeTypes[0] != GetCrateDataByUID(currentCrateID).type)
            {
                mergeMessageWindow.transform.Find("Text").GetComponent<Text>().text = "Only crates of same type can be combined!";
                mergeMessageWindow.SetActive(true);
                buttonsUnlocked = false;
                ResetMergeCrates();
                RefreshHighlights();
                return;
            }
            //If the crates are of the same type, the confirmation window is opened - See MergeConfirmButton function
            if(cratesToMergeTypes[0] == crates[1].tag)
            {
                cratesToMergeIDs[1] = currentCrateID;
                cratesToMergeTypes[1] = GetCrateDataByUID(currentCrateID).type;

                crates[1].transform.Find("crate").Find("crate_body").GetComponent<MeshRenderer>().materials[1].SetInt("_Enable", 1);
                crates[1].transform.Find("crate").Find("crate_body").Find("crate_cover").GetComponent<MeshRenderer>().materials[1].SetInt("_Enable", 1);

                mergeConfirmWindow.SetActive(true);
                buttonsUnlocked = false;

                Debug.Log($"Second Crate Selected with ID: {cratesToMergeIDs[1]} and type: {cratesToMergeTypes[1]}");
                return;
            }   
        }

        //TODO: Send the request to update the DB and after confirmation update the local data and spawn the new crate with the VFX, and remove the consumed crate with a fade out animation (Tested in ProcessMergeAction Coroutine)
    }
    //Highlight the selected crate and the next one to merge with, if the first selection is correct, otherwise shows an error message - See MergeCrates function
    void SetCrateHighlight(GameObject crate, bool enabled)
    {
        var body = crate.transform.Find("crate").Find("crate_body").GetComponent<MeshRenderer>();
        var cover = crate.transform.Find("crate").Find("crate_body").Find("crate_cover").GetComponent<MeshRenderer>();

        body.materials[1].SetInt("_Enable", enabled ? 1 : 0);
        cover.materials[1].SetInt("_Enable", enabled ? 1 : 0);
    }
    void RefreshHighlights()
    {
        for(int i = 0; i < crates.Count; i++)
        {
            int crateIndex = GetDataIndexForVisualSlot(i);

            bool shouldHighlight = 
                crateIndex == cratesToMergeIDs[0] || crateIndex == cratesToMergeIDs[1];
            SetCrateHighlight(crates[i], shouldHighlight);
        }
    }
    int GetDataIndexForVisualSlot(int visualIndex)
    {
        if(visualIndex == 1)
            return currentCrateID;

        if(visualIndex == 0)
            return GetPreviousCrateUID();

        if(visualIndex == 2)
            return GetNextCrateUID();
        
        return -1; // Invalid index
    }
    public void CloseMergeWindow(){
        ResetMergeCrates();
        RefreshHighlights();
        mergeMessageWindow.SetActive(false);
        mergeConfirmWindow.SetActive(false);
        buttonsUnlocked = true;
    }
    public void MergeConfirmButton(){
        mergeConfirmWindow.SetActive(false);
        StartCoroutine(ProcessMergeAction());
    }

    IEnumerator ProcessMergeAction(){
        //Adds VFX to the current Crate
        GameObject vfx = Instantiate(crateBurningVFX);
        vfx.transform.SetParent(crates[1].transform.Find("crate").Find("crate_body").transform);
        
        //Delay a bit before replacind the crate
        yield return new WaitForSeconds(1.6f);

        //Test which type the new crate will be
        string newCrateType;

        switch (cratesToMergeTypes[0]){
            case "Common": newCrateType = "Uncommon"; break;
            case "Uncommon": newCrateType = "Rare"; break;
            case "Rare": newCrateType = "Epic"; break;
            case "Epic": newCrateType = "Legendary"; break;
            default: newCrateType = "Common"; break;
        }

        GameObject oldCrate = crates[1];
        Vector3 pos = oldCrate.transform.position;
        Quaternion rot = oldCrate.transform.rotation;


        //Get the current time animation so the new crate starts the animation at the same frame.
        //Next the new crate is instantiated and the old one is replaced, and the animation is started at the correct frame 
        float time = oldCrate.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime;
        
        crates[1] = Instantiate(GetCratePrefabByType(newCrateType), pos, rot, transform);
        crates[1].GetComponent<Animator>().Play("Turn", 0 ,time);
        vfx.transform.SetParent(crates[1].transform.Find("crate").Find("crate_body").transform);
        
        Destroy(oldCrate);

        /*
         Test which of the two merged crates was in the previous slot and which one was in the next slot
         to understand how to update the currentCrateID after removing the consumed crate from the data list,
          and also for the animation of the burning crate
        */
        bool isOffScreen = true;
        bool firstSelectedWasPrevious = false;
        string crate1Name = $"Crate_{cratesToMergeIDs[0]}";

        for(int i = 0; i < crates.Count; i++)
        {
            string crateGameObjectName = crates[i].name;

            if(crateGameObjectName.Equals(crate1Name)){
                isOffScreen = false;
                firstSelectedWasPrevious = i == 0? true : false;
                break;
            }
        }

        Debug.Log($"crate 1: {cratesToMergeIDs[0]}, crate 2: {cratesToMergeIDs[1]}, isOffScreen: {isOffScreen}, firstSelectedWasPrevious: {firstSelectedWasPrevious}");

        //Updates the new crate data (Here is where the DB should be updated)
        GetCrateDataByUID(cratesToMergeIDs[1]).type = newCrateType;
        dummyCratesData.data.crates.RemoveAt(GetCrateIndexByUID(cratesToMergeIDs[0]));
        currentCrateID = cratesToMergeIDs[1];
      
        if(!isOffScreen){
            yield return new WaitForSeconds(2f);
        
            //Play the burning crate animation
            int CrateSlotID = (firstSelectedWasPrevious)? 0 : 2;
            crates[CrateSlotID].GetComponent<Animator>().SetBool("Fade", true);
            ResetMergeCrates();
            RefreshHighlights();

            yield return new WaitForSeconds(2f);
            
            bool hasEmptyCrate = EnsureEmptyCrate();
            
            int newCrateSpawnID = firstSelectedWasPrevious? GetPreviousCrateUID() : GetNextCrateUID();
            Crate newCrateData = GetCrateDataByUID(newCrateSpawnID);

            GameObject oldPrevCrate = crates[CrateSlotID];
            Vector3 oldPrevCratePos = oldPrevCrate.transform.position;
            Quaternion oldPrevCrateRot = oldPrevCrate.transform.rotation;

            crates[CrateSlotID] = Instantiate(GetCratePrefabByType(newCrateData.type), oldPrevCratePos, oldPrevCrateRot, transform);
            crates[CrateSlotID].transform.position = SpawnLocations[CrateSlotID].transform.position;

            if(newCrateData.status == "Opened"){
                crates[CrateSlotID].GetComponent<Animator>().SetBool("Open", true);
                crates[CrateSlotID].GetComponent<Animator>().Play("ItemFloating", 0, 0.0f);
            }

            Destroy(oldPrevCrate);
        }
        else
        {
            yield return new WaitForSeconds(2f);
            ResetMergeCrates();
            RefreshHighlights();
        }

        //Destroy leftover objects
        Destroy(vfx);

        //Update Names
        crates[0].name = $"Crate_{GetPreviousCrateUID()}";
        crates[1].name = $"Crate_{currentCrateID}";
        crates[2].name = $"Crate_{GetNextCrateUID()}";

        //Reset Control variables
        cratesToMergeIDs[0] = -1;
        cratesToMergeIDs[1] = -1;
        cratesToMergeTypes[0] = "";
        cratesToMergeTypes[1] = "";
        buttonsUnlocked = true;

        yield return null;
    }
    bool EnsureEmptyCrate(){
        //TODO
        return false;
    }
    public void DestroyVFX(){
        Destroy(this.gameObject);
    }
    // Dummy crates class for data handle --------------------------------------------

    [System.Serializable]
    public class CratesData{
        public Crates data;
    }

    [System.Serializable]
    public class Crates{
        public List<Crate> crates = new List<Crate>();
    }

    [System.Serializable]
    public class Crate{
        public int uid;
        public string type;
        public string status;

        public Crate(int _uid, string _type, string _status){
            uid = _uid;
            type = _type;
            status = _status;
        }
    }
    // END OF Dummy crates class for data handle --------------------------------------------
}
