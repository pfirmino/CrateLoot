using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;
using TMPro;

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
        HandleDummyData();

        currentCrateID = 0;
        buttonsUnlocked = true;

        SpawnCrates();

        currentdummyDatabaseSelector = dummyDatabaseSelector;
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
            currentCrateID = 0;
            buttonsUnlocked = true;
            ResetCrateSystem();
            HandleDummyData();
            SpawnCrates();
        }
    }

    void HandleDummyData(){
        if(dummyDatabaseSelector == DUMMY_DATABASE_SELECTOR.DATABASE1)
            dummyCratesDataJSON = "{\"data\": { \"crates\": [{\"id\": \"001\", \"type\": \"Common\"}, {\"id\": \"002\", \"type\": \"Uncommon\"}, {\"id\": \"003\", \"type\": \"Rare\"}, {\"id\": \"004\", \"type\": \"Epic\"}, {\"id\": \"005\", \"type\": \"Legendary\"}, {\"id\": \"006\", \"type\": \"Common\"}, {\"id\": \"007\", \"type\": \"Common\"}]}}";
        
        else if(dummyDatabaseSelector == DUMMY_DATABASE_SELECTOR.DATABASE2)
            dummyCratesDataJSON = "{\"data\": { \"crates\": [{\"id\": \"001\", \"type\": \"Common\"}]}}";
        
        else if(dummyDatabaseSelector == DUMMY_DATABASE_SELECTOR.DATABASE3)
            dummyCratesDataJSON = "{\"data\": { \"crates\": []}}";
   
        dummyCratesData = JsonUtility.FromJson<CratesData>(dummyCratesDataJSON);

        //If length is less then 3, adds empty crate placeholders

        if(dummyCratesData.data.crates.Count < 3){
            int InitialLength = dummyCratesData.data.crates.Count;

            for(int i = 3; i - InitialLength > 0; i--){
                dummyCratesData.data.crates.Add(new Crate(0, "Empty"));
            }
            Debug.Log(JsonUtility.ToJson(dummyCratesData));
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
        cratesToMergeIDs[0] = -1;
        cratesToMergeIDs[1] = -1;
        cratesToMergeTypes[0] = "";
        cratesToMergeTypes[1] = "";

        // Reset UI
        mergeMessageWindow.SetActive(false);
        mergeConfirmWindow.SetActive(false);

        // Reset control
        currentCrateID = 0;
        buttonsUnlocked = true;
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

    }
    public void OnSwitchDummyData(){
        Debug.Log("Dropdown changed to: " + UI_DBSelector.options[UI_DBSelector.value].text);
        int index = UI_DBSelector.value;
        dummyDatabaseSelector = (DUMMY_DATABASE_SELECTOR)index;
    }
    public void OpenButton(){
        if(crates[1].tag != "Empty")
            StartCoroutine(OpenAction());
    }

    IEnumerator OpenAction(){
        buttonsUnlocked = false;
        crates[1].GetComponent<Animator>().SetBool("Open", true);
        yield return new WaitForSeconds(2f);
        buttonsUnlocked = true;
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

    IEnumerator SpawnNextCrate(){
        currentCrateID++;

        if(currentCrateID > dummyCratesData.data.crates.Count-1)
            currentCrateID = 0;

        int NextCrateID = (currentCrateID + 1 > dummyCratesData.data.crates.Count-1)? 0 : currentCrateID + 1;

        GameObject previousCrate = crates[0];
        previousCrate.GetComponent<Animator>().SetBool("Out", true);

        yield return new WaitForSeconds(0.25f);
        crates.RemoveAt(0);
        GameObject newCrate = GameObject.Instantiate(GetCrateByType(dummyCratesData.data.crates[NextCrateID].type));
        newCrate.transform.position = SpawnLocations[2].transform.position;
        newCrate.transform.SetParent(transform);

        crates.Insert(2, newCrate);
        yield return new WaitForSeconds(1/transitionSpeed);
        GameObject.Destroy(previousCrate);

        yield return new WaitForSeconds(0.25f);
        buttonsUnlocked = true;
        
        yield return null;
    }

    IEnumerator SpawnPreviousCrate(){
        currentCrateID--;

        if(currentCrateID < 0)
            currentCrateID = dummyCratesData.data.crates.Count-1;

        int PreviousCrateID = (currentCrateID - 1 < 0)? dummyCratesData.data.crates.Count-1 : currentCrateID - 1;

        crates[2].GetComponent<Animator>().SetBool("Out", true);

        yield return new WaitForSeconds(0.25f);
        GameObject newCrate = GameObject.Instantiate(GetCrateByType(dummyCratesData.data.crates[PreviousCrateID].type));
        newCrate.transform.position = SpawnLocations[0].transform.position;
        newCrate.transform.SetParent(transform);

        crates.Insert(0, newCrate);

        yield return new WaitForSeconds(2/transitionSpeed);
        GameObject.Destroy(crates[3]);
        crates.RemoveAt(3);

        yield return new WaitForSeconds(0.25f);
        buttonsUnlocked = true;
        
        yield return null;
    }

    public void MergeCrates()
    {
        if(buttonsUnlocked)
        {
            if(crates[1].tag == "Empty")
                return;

            //Select the first Crate to Merge
            if(cratesToMergeIDs[0] == -1){
                cratesToMergeIDs[0] = currentCrateID;
                cratesToMergeTypes[0] = dummyCratesData.data.crates[currentCrateID].type;

                crates[1].transform.Find("crate").Find("crate_body").GetComponent<MeshRenderer>().materials[1].SetInt("_Enable", 1);
                crates[1].transform.Find("crate").Find("crate_body").Find("crate_cover").GetComponent<MeshRenderer>().materials[1].SetInt("_Enable", 1);
                
                return;
            }
            //Check if user's trying to merge the same crate with itself
            if(currentCrateID == cratesToMergeIDs[0]){
                mergeMessageWindow.transform.Find("Text").GetComponent<Text>().text = "You can't merge the same crate.";
                mergeMessageWindow.SetActive(true);
                buttonsUnlocked = false;
                return;
            }
            //Check if user's trying of different types
            if(cratesToMergeTypes[0] != dummyCratesData.data.crates[currentCrateID].type)
            {
                mergeMessageWindow.transform.Find("Text").GetComponent<Text>().text = "Only crates of same type can be combined!";
                mergeMessageWindow.SetActive(true);
                buttonsUnlocked = false;
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
            case "Common":
                newCrateType = "Uncommon";
                break;
            case "Uncommon":
                newCrateType = "Uncommon";
                break;
            case "Rare":
                newCrateType = "Epic";
                break;
            case "Epic":
                newCrateType = "Legendary";
                break;
            default:
                newCrateType = "Common";
                break;
        }

        //Get the current time animation so the new crate starts the animation at the same frame.
        //Next the new crate is instantiated and the old one is replaced, and the animation is started at the correct frame 
        float time = crates[1].GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime;
        Destroy(crates[1]);
        crates[1] = Instantiate(GetCrateByType(newCrateType));
        crates[1].GetComponent<Animator>().Play("Turn", 0 ,time);
        vfx.transform.SetParent(crates[1].transform.Find("crate").Find("crate_body").transform);

        //Updates the new crate data (Here is where the DB should be updated)
        dummyCratesData.data.crates[cratesToMergeIDs[1]].type = newCrateType;
        dummyCratesData.data.crates.RemoveAt(cratesToMergeIDs[0]);



        yield return new WaitForSeconds(2f);
        
        //Test if the first crate selected was the next one visible, if so fades it out and remove it
        int nextID = (currentCrateID + 1 > dummyCratesData.data.crates.Count-1)? 0 : currentCrateID + 1;

        if(cratesToMergeIDs[0] == nextID){
            //Fix the current Crate ID after removing the consumed crate
            if(currentCrateID > cratesToMergeIDs[0]){
                currentCrateID--;
                nextID = (currentCrateID + 1 > dummyCratesData.data.crates.Count-1)? 0 : currentCrateID + 1;
            }

            crates[2].GetComponent<Animator>().SetBool("Fade", true);
            crates[2].transform.Find("crate").Find("crate_body").GetComponent<MeshRenderer>().materials[1].SetInt("_Enable", 0);
            crates[2].transform.Find("crate").Find("crate_body").Find("crate_cover").GetComponent<MeshRenderer>().materials[1].SetInt("_Enable", 0);
            
            yield return new WaitForSeconds(2f);
            
            GameObject remove = crates[2];
            crates[2] = Instantiate(GetCrateByType(dummyCratesData.data.crates[nextID].type));
            crates[2].transform.position = SpawnLocations[2].transform.position;
            Destroy(remove); 
        }

        //Test if the first crate selected was the previous one visible, if so fades it out and remove it
        int previousID = (currentCrateID - 1 < 0)? dummyCratesData.data.crates.Count-1 : currentCrateID - 1;

        if(cratesToMergeIDs[0] == previousID){
            //Fix the current Crate ID after removing the consumed crate
            if(currentCrateID > cratesToMergeIDs[0]){
                currentCrateID--;
                previousID = (currentCrateID - 1 < 0)? dummyCratesData.data.crates.Count-1 : currentCrateID - 1;
            }

            crates[0].GetComponent<Animator>().SetBool("Fade", true);
            crates[0].transform.Find("crate").Find("crate_body").GetComponent<MeshRenderer>().materials[1].SetInt("_Enable", 0);
            crates[0].transform.Find("crate").Find("crate_body").Find("crate_cover").GetComponent<MeshRenderer>().materials[1].SetInt("_Enable", 0);

            yield return new WaitForSeconds(2f);

            GameObject remove = crates[0];
            crates[0] = Instantiate(GetCrateByType(dummyCratesData.data.crates[previousID].type));
            crates[0].transform.position = SpawnLocations[0].transform.position;
            Destroy(remove); 
        }

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

        public Crate(int _id, string _type){
            id = _id;
            type = _type;
        }
    }
    // END OF Dummy crates class for data handle --------------------------------------------
}
