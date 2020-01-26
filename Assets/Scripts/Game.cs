using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Game : MonoBehaviour
{
    [SerializeField] private Camera MainCamera;
    [SerializeField] private Character Character;
    [SerializeField] private Canvas Menu;
    [SerializeField] private GameObject Hud;
    [SerializeField] private GameObject BattleHud;
    [SerializeField] private Transform CharacterStart;

    private RaycastHit[] mRaycastHits;
    private Character mCharacter;
    private Environment mMap;

    //Starts outline of tile on this tile
    private readonly Vector3 NodeSize = Vector3.one * 9.0f;
    private readonly int NumberOfRaycastHits = 1;

    //My Variables
    private EnvironmentTile newTile;

    private List<EnvironmentTile> SpellTiles { get; set; }

    private int APCost { get; set; }
    private float Timer = 0;
    private bool followCharacter = false;
    public int Score;
    public bool CoroutineRunning = false;
    public bool BattleRunning = false;

    void Start()
    {
        mRaycastHits = new RaycastHit[NumberOfRaycastHits];
        SpellTiles = new List<EnvironmentTile>(0);
        mMap = GetComponentInChildren<Environment>();
        mCharacter = Instantiate(Character, transform);
        mCharacter.game = this;
        mCharacter.Hud = Hud.GetComponent<HUD>();
        mCharacter.CurrentArea = mMap;
        Hud.GetComponent<HUD>().NextWave.gameObject.SetActive(false);
        Score = 0;
        ShowMenu(true);
    }

    private void Update()
    {
        // Check to see if the player has clicked a tile and if they have, try to find a path to that 
        // tile. If we find a path then the character will move along it to the clicked tile. 
        if(Input.GetMouseButtonDown(0) && !CoroutineRunning)
        {
            Ray screenClick = MainCamera.ScreenPointToRay(Input.mousePosition);
            int hits = Physics.RaycastNonAlloc(screenClick, mRaycastHits);
            if( hits > 0)
            {
                if (BattleRunning == true && APCost <= mCharacter.AP || BattleRunning == false)
                {
                    CoroutineRunning = true;
                    if (BattleRunning == true)
                    {
                        mCharacter.AP -= APCost;
                        Hud.GetComponent<HUD>().UpdateAPAvailibilty(mCharacter.AP);
                    }
                    EnvironmentTile tile = mRaycastHits[0].transform.GetComponent<EnvironmentTile>();
                    if (tile != null)
                    {

                        List<EnvironmentTile> route = mMap.Solve(mCharacter.CurrentPosition, tile);
                        mCharacter.GoTo(route);
                    }
                }
                else
                {
                    //Display not enough AP to walk
                }
            }
        }

        //Outline hovered Tile
        Ray screenHover = MainCamera.ScreenPointToRay(Input.mousePosition);
        int inMenu = Physics.RaycastNonAlloc(screenHover, mRaycastHits);
        if (inMenu > 0)
        {
            EnvironmentTile newTileHover = mRaycastHits[0].transform.GetComponent<EnvironmentTile>();
            if (newTileHover != null && newTileHover != newTile && newTileHover != mCharacter.CurrentPosition && !CoroutineRunning)
            {
                if (newTile != null && newTile.IsAccessible != false)
                {
                    RegulateNewTile(newTile, false, 0);
                }     
                RegulateNewTile(newTileHover, false, 1);
                if (BattleRunning == true && mMap.Solve(mCharacter.CurrentPosition, newTileHover) != null)
                {
                    float Cost = (mMap.Solve(mCharacter.CurrentPosition, newTileHover).Count - 1f) / 2f;
                    if (Cost % 1f == 0.5f)
                        Cost += 0.5f;
                    APCost = (int)Cost;
                    Hud.GetComponent<HUD>().UpdateAPCost((int)Cost);
                }
            }
        }

        if (followCharacter == true)
        {
            MainCamera.transform.position = mCharacter.transform.position + new Vector3(0,189,-300);
        }

        //Check to see if Enemies alive in area
        if (!Menu.enabled)
        {
            Hud.GetComponent<HUD>().UpdateScore(Score);
            if (BattleRunning == false && mCharacter.CurrentArea.Enemies.Count != 0)
            {
                BattleRunning = true;
                BattleHud.SetActive(true);
            }
            else if(BattleRunning == true && mCharacter.CurrentArea.Enemies.Count == 0)
            {
                BattleRunning = false;
                BattleHud.SetActive(false);
            }
            else if(BattleRunning == false && mCharacter.CurrentArea.Enemies.Count == 0 && Timer <= 3)
            {
                Timer += Time.deltaTime;
                Hud.GetComponent<HUD>().NextWave.gameObject.SetActive(true);
                Hud.GetComponent<HUD>().UpdateNextWave(3 - (int)Timer);
                if (Timer > 3)
                {
                    mMap.NewEnemies();
                    Timer = 0;
                    mCharacter.AP = 4;
                    Hud.GetComponent<HUD>().NextWave.gameObject.SetActive(false);
                }
            }

            if (mCharacter.CurrentPosition.IsPickupable == true)
            {
                RegulateNewTile(mCharacter.CurrentPosition, false, 0);
                Score += 50;
            }
        }
    }

    private void RegulateNewTile(EnvironmentTile oldTile, bool onGeneration, int tileType)
    {
        if (oldTile.IsPickupable == true && mCharacter.CurrentPosition == oldTile)
        {
            oldTile.Random = 0;
            oldTile.IsPickupable = false;
        }
        newTile = mMap.GenerateTile(onGeneration, tileType, oldTile.Random, oldTile.Position, oldTile.Rotation, oldTile.X, oldTile.Y, oldTile.IsAccessible, oldTile.IsPickupable);
        if (mCharacter.CurrentPosition == oldTile)
        {
            mCharacter.CurrentPosition = newTile;
        }
        for (int j = 0; j < mMap.Enemies.Count; j++)
        {
            if (mMap.Enemies[j].CurrentPosition == oldTile)
            {
                mMap.Enemies[j].CurrentPosition = newTile;
            }
        }
        for (int x = 0; x < oldTile.Connections.Count; x++)
        {
            oldTile.Connections[x].Connections.Add(newTile);
            oldTile.Connections[x].Connections.Remove(oldTile);
        }
        Destroy(oldTile.gameObject);
        mMap.Connect(newTile, newTile.X, newTile.Y);
    }

    private List<EnvironmentTile> GetAbility1Tiles()
    {
        if(SpellTiles != null)
            SpellTiles.Clear();
        EnvironmentTile CP = mCharacter.CurrentPosition;
        List<EnvironmentTile> C = CP.Connections;
        for (int i = 0; i < C.Count; i++)
        {
            SpellTiles.Add(C[i]);
            for (int j = 0; j <C[i].Connections.Count; j++)
            {
                if ((C[i].Connections[j].X == CP.X + 1 && C[i].Connections[j].Y == CP.Y + 1) || (C[i].Connections[j].X == CP.X + 1 && C[i].Connections[j].Y == CP.Y - 1) || 
                    (C[i].Connections[j].Y == CP.Y + 1 && C[i].Connections[j].X == CP.X - 1) || (C[i].Connections[j].Y == CP.Y - 1 && C[i].Connections[j].X == CP.X - 1))
                {
                    if(!SpellTiles.Contains(C[i].Connections[j]))
                        SpellTiles.Add(C[i].Connections[j]);
                }
            }
        }
        return SpellTiles;
    }

    private List<EnvironmentTile> GetAbility2Tiles()
    {
        if(SpellTiles != null)
            SpellTiles.Clear();
        EnvironmentTile CP = mCharacter.CurrentPosition;
        List<EnvironmentTile> C = CP.Connections;
        for (int i = 0; i < C.Count; i++)
        {
            SpellTiles.Add(C[i]);
            for (int j = 0; j < C[i].Connections.Count; j++)
            {
                if (C[i].Connections[j].X == CP.X + 2 || C[i].Connections[j].X == CP.X - 2 || C[i].Connections[j].Y == CP.Y + 2 || C[i].Connections[j].Y == CP.Y - 2 )
                {
                    if (!SpellTiles.Contains(C[i].Connections[j]))
                        SpellTiles.Add(C[i].Connections[j]);
                }
            }
        }
        return SpellTiles;
    }

    private int DisplayAbility1(int intent)
    {
        if(CoroutineRunning != true && intent == 1)
        {
            CoroutineRunning = true;
            //Find Correct Tiles and Change them
            RegulateNewTile(newTile, false, 0);
            GetAbility1Tiles();
            for(int i = 0; i < SpellTiles.Count; i++)
            {
                if(SpellTiles[i].IsAccessible == true)
                    RegulateNewTile(SpellTiles[i], false, 2);
                else
                    RegulateNewTile(SpellTiles[i], false, 4);
            }
        }
        else if(CoroutineRunning == true && intent == 1)
        {
            CoroutineRunning = false;
            intent = 0;
            //Undo
            GetAbility1Tiles();
            for (int i = 0; i < SpellTiles.Count; i++)
            {
                if (SpellTiles[i].IsAccessible == true)
                    RegulateNewTile(SpellTiles[i], false, 0);
                else
                    RegulateNewTile(SpellTiles[i], false, 3);
            }
        }
        else
        {
            //Display wait to finish action
        }
        return intent;
    }

    private int DisplayAbility2(int intent)
    {
        if (CoroutineRunning != true && intent == 2)
        {
            CoroutineRunning = true;
            //Find Correct Tiles and Change them
            RegulateNewTile(newTile, false, 0);
            GetAbility2Tiles();
            for (int i = 0; i < SpellTiles.Count; i++)
            {
                if (SpellTiles[i].IsAccessible == true)
                    RegulateNewTile(SpellTiles[i], false, 2);
                else
                    RegulateNewTile(SpellTiles[i], false, 4);
            }
        }
        else if (CoroutineRunning == true && intent == 2)
        {
            CoroutineRunning = false;
            intent = 0;
            //Undo
            GetAbility2Tiles();
            for (int i = 0; i < SpellTiles.Count; i++)
            {
                if (SpellTiles[i].IsAccessible == true)
                    RegulateNewTile(SpellTiles[i], false, 0);
                else
                    RegulateNewTile(SpellTiles[i], false, 3);
            }
        }
        else
        {
            //Display wait to finish action
        }
        return intent;
    }

    public void EndTurn()
    {
        CoroutineRunning = true;
        for (int i = 0; i < mMap.Enemies.Count; i++)
        {
            List<EnvironmentTile> route = mMap.Solve(mMap.Enemies[i].CurrentPosition, mCharacter.CurrentPosition);
            if (route != null && route.Count != 2)
            {
                route.RemoveAt(route.Count - 1);
                float routeCost = (route.Count - 1) / 2;
                if (routeCost % 1f == 0.5f)
                    routeCost += 0.5f;
                if (mMap.Enemies[i].AP < (int)routeCost)
                {
                    for (int j = 0; j < ((int)routeCost - mMap.Enemies[i].AP) * 2; j++)
                    {
                        route.RemoveAt(route.Count - 1);
                    }
                }
                mMap.Enemies[i].AP -= (int)routeCost;
                mMap.Enemies[i].GoTo(route);
            }
            for(int j = 0; j < mMap.Enemies[i].AP; j++)
            {
                //more AP means more damage
                mCharacter.TakeDamage(1);
                mMap.Enemies[i].AP -= 1;
            }
            mMap.Enemies[i].AP = 2;
        }

        mCharacter.AP = 4;
        Hud.GetComponent<HUD>().UpdateAPAvailibilty(4);
    }

    public void SetAbility1()
    {
        if (CoroutineRunning != true)
        {
            mCharacter.intent = 1;
        }
        if(BattleRunning == true)
        {
            APCost = 1;
            Hud.GetComponent<HUD>().UpdateAPCost(1);
        }
        mCharacter.intent = DisplayAbility1(mCharacter.intent);
    }

    public void SetAbility2()
    {
        if (CoroutineRunning != true)
        {
            mCharacter.intent = 2;
        }
        if (BattleRunning == true)
        {
            APCost = 1;
            Hud.GetComponent<HUD>().UpdateAPCost(1);
        }
        mCharacter.intent = DisplayAbility2(mCharacter.intent);
    }

    public void Use()
    {
        if (mCharacter.intent != 0)
        {
            if (mCharacter.intent == 1)
            {
                SpellTiles = GetAbility1Tiles();
                if (BattleRunning == true && APCost <= mCharacter.AP)
                {
                    mCharacter.AP -= APCost;
                    Hud.GetComponent<HUD>().UpdateAPAvailibilty(mCharacter.AP);
                    mCharacter.UseAbility(SpellTiles);
                }
                else
                    mCharacter.UseAbility(SpellTiles);
            }
            else if (mCharacter.intent == 2)
            {
                SpellTiles = GetAbility2Tiles();
                if (BattleRunning == true && APCost <= mCharacter.AP)
                {
                    mCharacter.AP -= APCost;
                    Hud.GetComponent<HUD>().UpdateAPAvailibilty(mCharacter.AP);
                    mCharacter.UseAbility(SpellTiles);
                }
                else
                    mCharacter.UseAbility(SpellTiles);
            }
        }
        else
        {
            //Display set ability message
        }
    }

    public void ShowMenu(bool show)
    {
        if (Menu != null && Hud != null)
        {
            Menu.enabled = show;
            Hud.SetActive(!show);
            BattleHud.SetActive(false);

            if (show)
            {
                mCharacter.transform.position = CharacterStart.position;
                mCharacter.transform.rotation = CharacterStart.rotation;
                mMap.CleanUpWorld();
                mCharacter.Reset();
                CoroutineRunning = false;
                BattleRunning = false;
                followCharacter = false;
                MainCamera.transform.position = new Vector3(-0.6f,189f,-302f);
                Hud.GetComponent<HUD>().NextWave.gameObject.SetActive(false);
                Score = 0;
            }
            else
            {
                mCharacter.transform.position = mMap.Start.Position;
                mCharacter.transform.rotation = Quaternion.identity;
                mCharacter.CurrentPosition = mMap.Start;
                followCharacter = true;
            }
        }
    }

    

    public void Generate()
    {
        //Multiple rooms needs to be implemented
        mMap.GenerateWorld(5, 0);  
    }

    public void Exit()
    {
        Application.Quit();
    }
}
