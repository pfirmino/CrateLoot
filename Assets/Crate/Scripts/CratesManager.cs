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
    Dictionary<int, int> cachedId = new Dictionary<int, int>();
    int currentCrateID;

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
            ResetCrateSystem();
            HandleDummyData();
            SpawnCrates();
        }
    }

    void HandleDummyData(){
        if(dummyDatabaseSelector == DUMMY_DATABASE_SELECTOR.DATABASE1)
            dummyCratesDataJSON = "{\"data\": { \"crates\": [{\"id\": \"001\", \"type\": \"Common\", \"status\": \"Closed\"}, {\"id\": \"002\", \"type\": \"Uncommon\", \"status\": \"Closed\"}, {\"id\": \"003\", \"type\": \"Rare\", \"status\": \"Closed\"}, {\"id\": \"004\", \"type\": \"Epic\", \"status\": \"Closed\"}, {\"id\": \"005\", \"type\": \"Legendary\", \"status\": \"Closed\"}, {\"id\": \"006\", \"type\": \"Common\", \"status\": \"Closed\"}, {\"id\": \"007\", \"type\": \"Common\", \"status\": \"Closed\"}]}}";
        
        else if(dummyDatabaseSelector == DUMMY_DATABASE_SELECTOR.DATABASE2)
            dummyCratesDataJSON = "{\"data\": { \"crates\": [{\"id\": \"001\", \"type\": \"Common\", \"status\": \"Closed\"}]}}";
        
        else if(dummyDatabaseSelector == DUMMY_DATABASE_SELECTOR.DATABASE3)
            dummyCratesDataJSON = "{\"data\": { \"crates\": []}}";
   
        dummyCratesData = JsonUtility.FromJson<CratesData>(dummyCratesDataJSON);

        //If length is less then 3, adds empty crate placeholders

        if(dummyCratesData.data.crates.Count < 3){
            int InitialLength = dummyCratesData.data.crates.Count;

            for(int i = 3; i - InitialLength > 0; i--){
                dummyCratesData.data.crates.Add(new Crate(0, "Empty", "Closed"));
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
        currentCrateID = 0;
        buttonsUnlocked = true;
    }
    void ResetMergeCrates(){
        cratesToMergeIDs[0] = -1;
        cratesToMergeIDs[1] = -1;
        cratesToMergeTypes[0] = "";
        cratesToMergeTypes[1] = "";
    }

    GameObject GetCrateByType(string _type){
        foreach(GameObject cratePrefab in cratePrefabs){
            if(cratePrefab.tag == _type){
                return cratePrefab;
            }
        }
        return cratePrefabs[0];
    }
    void SpawnCrates(){
        crates.Add( GameObject.Instantiate(GetCrateByType(dummyCratesData.data.crates[dummyCratesData.data.crates.Count-1].type)));
        crates.Add( GameObject.Instantiate(GetCrateByType(dummyCratesData.data.crates[0].type))); 
        crates.Add( GameObject.Instantiate(GetCrateByType(dummyCratesData.data.crates[1].type))); 
        
        crates[0].transform.position = SpawnLocations[0].transform.position;
        crates[0].transform.SetParent(transform);
        
        crates[1].transform.position = SpawnLocations[1].transform.position;
        crates[1].transform.SetParent(transform);
        
        crates[2].transform.position = SpawnLocations[2].transform.position;
        crates[2].transform.SetParent(transform);
        
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
        dummyCratesData.data.crates[currentCrateID].status = "Opened";
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
    int GetNextCrateID(){
        return (currentCrateID + 1 > dummyCratesData.data.crates.Count-1)? 0 : currentCrateID + 1;
    }
    int GetPreviousCrateID(){
        return (currentCrateID - 1 < 0)? dummyCratesData.data.crates.Count-1 : currentCrateID - 1;
    }

    IEnumerator SpawnNextCrate(){
        currentCrateID++;

        if(currentCrateID > dummyCratesData.data.crates.Count-1)
            currentCrateID = 0;

        GameObject previousCrate = crates[0];
        previousCrate.GetComponent<Animator>().SetBool("Out", true);

        yield return new WaitForSeconds(0.25f);
        crates.RemoveAt(0);
        GameObject newCrate = GameObject.Instantiate(GetCrateByType(dummyCratesData.data.crates[GetNextCrateID()].type));
        newCrate.transform.position = SpawnLocations[2].transform.position;
        newCrate.transform.SetParent(transform);

        if(dummyCratesData.data.crates[GetNextCrateID()].status == "Opened"){
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
        currentCrateID--;

        if(currentCrateID < 0)
            currentCrateID = dummyCratesData.data.crates.Count-1;

        crates[2].GetComponent<Animator>().SetBool("Out", true);

        yield return new WaitForSeconds(0.25f);
        GameObject newCrate = GameObject.Instantiate(GetCrateByType(dummyCratesData.data.crates[GetPreviousCrateID()].type));
        newCrate.transform.position = SpawnLocations[0].transform.position;
        newCrate.transform.SetParent(transform);

        if(dummyCratesData.data.crates[GetPreviousCrateID()].status == "Opened"){
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
            if(crates[1].tag == "Empty")
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
                cratesToMergeTypes[0] = dummyCratesData.data.crates[currentCrateID].type;
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
            if(cratesToMergeTypes[0] != dummyCratesData.data.crates[currentCrateID].type)
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
                cratesToMergeTypes[1] = dummyCratesData.data.crates[currentCrateID].type;

                crates[1].transform.Find("crate").Find("crate_body").GetComponent<MeshRenderer>().materials[1].SetInt("_Enable", 1);
                crates[1].transform.Find("crate").Find("crate_body").Find("crate_cover").GetComponent<MeshRenderer>().materials[1].SetInt("_Enable", 1);

                mergeConfirmWindow.SetActive(true);
                buttonsUnlocked = false;
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
            return (currentCrateID - 1 < 0)? dummyCratesData.data.crates.Count-1 : currentCrateID - 1;
        if(visualIndex == 2)
            return (currentCrateID + 1 > dummyCratesData.data.crates.Count-1)? 0 : currentCrateID + 1;
        
        return -1; // Invalid index
    }
    public void CloseMergeWindow(){
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

        //Get the current time animation so the new crate starts the animation at the same frame.
        //Next the new crate is instantiated and the old one is replaced, and the animation is started at the correct frame 
        float time = crates[1].GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime;
        Destroy(crates[1]);
        crates[1] = Instantiate(GetCrateByType(newCrateType));
        crates[1].GetComponent<Animator>().Play("Turn", 0 ,time);
        vfx.transform.SetParent(crates[1].transform.Find("crate").Find("crate_body").transform);

        //Handle infinit loop of merging the same crate by checking the direction of the caroussel 
        //and which one of the two merged crates was in the previous slot and which one in the next slot, 
        //to update the currentCrateID accordingly and spawn the new crate in the correct slot
        int CarousselDirection = cratesToMergeIDs[1] - cratesToMergeIDs[0];
        if(cratesToMergeIDs[0] == 0 && cratesToMergeIDs[1] == 1)
            CarousselDirection = -1;
        
        if(cratesToMergeIDs[0] == 1 && cratesToMergeIDs[1] == 0)
            CarousselDirection = 1;
        
        bool firstSelectedWasPrevious = CarousselDirection < 0;


        //Updates the new crate data (Here is where the DB should be updated)
        dummyCratesData.data.crates[cratesToMergeIDs[1]].type = newCrateType;
        dummyCratesData.data.crates.RemoveAt(cratesToMergeIDs[0]);

        //After removing the consumed crate from the data list, the currentCrateID needs to be updated
        // to point to the correct crate, depending on which one was consumed 
        // (The one in the previous slot or the next slot)
        if (firstSelectedWasPrevious && currentCrateID != 0)
            currentCrateID = currentCrateID - 1;

        else if (currentCrateID > dummyCratesData.data.crates.Count - 1)
            currentCrateID = dummyCratesData.data.crates.Count - 1;
        

        yield return new WaitForSeconds(2f);
    
        //Play the burning crate animation
        int CrateSlotID = (firstSelectedWasPrevious)? 0 : 2;
        crates[CrateSlotID].GetComponent<Animator>().SetBool("Fade", true);
        ResetMergeCrates();
        RefreshHighlights();

        yield return new WaitForSeconds(2f);
        
        bool hasEmptyCrate = EnsureEmptyCrate();
        
        int newCrateSpawnID = firstSelectedWasPrevious? GetPreviousCrateID() : GetNextCrateID();

        GameObject remove = crates[CrateSlotID];
        crates[CrateSlotID] = Instantiate(GetCrateByType(dummyCratesData.data.crates[newCrateSpawnID].type));
        crates[CrateSlotID].transform.position = SpawnLocations[CrateSlotID].transform.position;

        if(dummyCratesData.data.crates[newCrateSpawnID].status == "Opened"){
            crates[CrateSlotID].GetComponent<Animator>().SetBool("Open", true);
            crates[CrateSlotID].GetComponent<Animator>().Play("ItemFloating", 0, 0.0f);
        }

        Destroy(remove);

        //Destroy leftover objects
        Destroy(vfx);

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
        public int id;
        public string type;
        public string status;

        public Crate(int _id, string _type, string _status){
            id = _id;
            type = _type;
            status = _status;
        }
    }
    // END OF Dummy crates class for data handle --------------------------------------------
}
