# P2E Doss Games

Intro
-----

Doss is the world's first platform where developers and creators can build and launch their own Play2Earn games. It is currently in the alpha stage, so creators would need scripting knowledge; but eventually, it will be a no-code experience.

  

The infra to ship the economics of the games to the blockchain main net is under development and we will very soon leverage that to the open-source community as well.

  

How to share new unique game ideas
----------------------------------

1.  You can share your unique game ideas as a new issue in the repository.
2.  We have an issue template attached that will make your life easier 🙂 .
3.  For making your game ideas more visible attach the `gameIdea` tag, additionally, you can attach more tags like `shooting` , `racing` , `treasure-hunt` for adding more context!

  

How to publish your games
-------------------------

Publishing your own Play to Earn Game on Doss currently requires some scripting and programming knowledge. We are working on making it more streamlined and easier for non-programmers to publish their own game.

  

Meanwhile, if you know little bits of C# (or similar OOP language), voila! 90 percent of your work is done.

  

In order to add a new game on the Doss platform, you simply need to implement the MiniGameBase class, details of which are provided in the following sections.

  

For the in-game assets, you can use the variety of assets available in the <DOSS Asset Store/Folder> for the games you create. Each asset has some properties configured that can be sent and received upon collision with other in-game assets.

  

To make it easier to understand, we have also added a sample game in <Examples> section with a complete walkthrough.

  

  

#### GameBase class structure:

  

```c#
public class MiniGameBase
{
//This dictionary holds the asset_id and the corresponding smart object. This is later used to retrieve the smart objects corresponding to the entity for triggering appropriate actions when the entities collide.
    public Dictionary<string, SmartObjectInteractionBase> smartObjects = new Dictionary<string, SmartObjectInteractionBase>();


//Initiate the placement of assets and broadcast it to other players.
    public virtual void PlaceAssets(){}
    
// Initiates what inventory should be equipped by the player when the game starts.
    public virtual void EquipInventory(){}
    
// Unequips all the inventory.
    public virtual void UnequipInventory(){}
    
// Call this function if you want to enable the primary button and pass the appropriate onclick function argument.
    public void EnablePrimaryButton(string buttonText, Action onClick){}

//This is called when photon room properties are updated. Handle states for the multiplayer events such as creating assets and updating asset properties, etc. The `start_game` property is available in the change properties whenever the master client starts the game. 
    public virtual void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged){}

// This is called whenever any player properties are updated.
    public virtual void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps){}


// This is called on every frame. You can use this to update local properties. Any parameter which needs to be handled on each frame. For example, whenever the player dies, it is updated via this function.
    public virtual void GameUpdate(){}

// You can add additional logic that you would like to end your game.
    public virtual void GameClearObject(){}

// Call this function to create game entities like trees, cars, gems, etc.
    public EntityData AddEntity(short id, string bundle, string asset, Vector3 position, float3 scale, bool undoRedoTask = false,
        byte _colorOverlay = 0x0, byte _physicsProperties = 0xE3, byte _additionalProperties = 0xF){}

}
```

  

### Initiating Doss

1.  Clone the project.
2.  Make a new C# file in the `/Games` directory folder.
3.  Use the given template to build your own game.

####   

### Publish the game

1.  Open a pull request and fill out the template
2.  Your game is in the pipeline!

  

Sample Game
-----------

```c#
 // MIT License

// Copyright (c) 2022 dossgames

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE. 
 
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Unity.Entities;
using Unity.Mathematics;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Transforms;

public class Shooting: MiniGameBase
{
    List<Tuple<string, string>> bullets = new List<Tuple<string, string>>();
    List<Tuple<string, string>> obstacles = new List<Tuple<string, string>>();

    string bulletAsset = "";
    string bulletBundle = "";

    public Shooting(Dictionary<string, object> gameAssetInput) : base(gameAssetInput)
    {   
        // Parsing game asset input for identifying which assets to add to the game.
        // This helps in decoupling the game logic (like asset placement and win condition)
        // with the assets used. This way, different games with logic and assets combination can
        // be created seamlessly. 
        foreach (var gameAsset in gameAssetInput)
        {
            if (gameAsset.Key.Contains("obstacle")) obstacles.Add((Tuple<string, string>)(gameAsset.Value));
            if (gameAsset.Key.Contains("bullet"))
            {
                var t = (Tuple<string, string>)(gameAsset.Value);
                bulletAsset = t.Item2;
                bulletBundle = t.Item1;
                
                // Although each in-game asset can be represented by a separate SmartObject, all bullets share a common behavior and 
                // created and destroyed frequently. Therefore it makes sense to store it only once.
                smartObjects["asset_100001"] = AssetRegistry.GetSmartObjectFromData(bulletAsset, new SmartObjectData { id = 100001, sendingProperties = 3, damageSend = 10, receivingProperties = (int)MiniGameUtils.Properties.POINT });
            }
        }
    }

    public void ResetPlayerProperties()
    {   
        // We want the game to be FPS.
        GameManager.instance.SwitchToFppView();
        
        // Treat player as a smartObject. This way, all collision interaction with game assets can be processed. 
        // Can skip if you dont want the player to interact with any in-game asset
        if (entityManager.HasComponent<SmartObjectData>(GameManager.instance.playerEntity))
        {
            entityManager.SetComponentData<SmartObjectData>(GameManager.instance.playerEntity, new SmartObjectData { id = 10000 });
        }
        else
        {
            entityManager.AddComponentData(GameManager.instance.playerEntity, new SmartObjectData { id = 10000 });
        }

        smartObjects["asset_10000"] = AssetRegistry.GetSmartObject("player", GameManager.instance.playerEntity);
        
        // IMPORTANT: Reset player score to zero before the start of game.
        Player player = PhotonNetwork.LocalPlayer;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "score", 0 } });
    }

    public override void PlaceAssets()
    {   
        // Hashtable to be used for broadcasting in-game asset information to other players
        // using Photon Room properties
        var assetHashtable = new ExitGames.Client.Photon.Hashtable { };
        
        // Generate 20 random spots on the Map where in-game assets can be placed 
        List<Vector3> assetPlaceholders = PlaceholderManager.instance.Placeholders.GetRandomItems(20);
  
        // Each in-game asset should be assigned a unique Id. We will simply start from zero and 
        // increment it as we go on and add new assets. 
        short id = 0;
  
        foreach (Vector3 assetPosition in assetPlaceholders)
        {
            if (obstacles.Count > 0)
            {
                var obstacleTup = obstacles[UnityEngine.Random.Range(0, obstacles.Count)];
  
                // Use the "AddEntity" helper function to create in-game assets given the asset and bundle name and 
                // place them at the given "assetPosition"
                var temp = AddEntity(id, obstacleTup.Item1, obstacleTup.Item2, assetPosition, new float3(1, 1, 1), false);
                id++;
                
                // Manually add to hashtable, so that the asset can be created for other players as well with similar properties
                assetHashtable.Add(temp.key, JsonUtility.ToJson(new PhotonSmartObjectDetails(smartObjects[temp.key], obstacleTup.Item2, obstacleTup.Item1, assetPosition, temp.rotation)));
            }
        }
        
        // Set assetHastable created to the Photon room properties. These need to be parsed and converted to new assets on each player's end
        PhotonNetwork.CurrentRoom.SetCustomProperties(assetHashtable);

    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        
        // "start_game" is set every time the masterClient starts the game for all players in the room
        // This should be the trigger to perform necesarry set and reset for the game.
        if (propertiesThatChanged.TryGetValue("start_game", out object status))
        { 
            // Required: Reset player properties and score to zero to avoid any scores from being carried over from previous game
            ResetPlayerProperties();
            // Equip any inventory need for the game
            EquipInventory();
            // Use this helper to enable any button for the gameControl
            EnablePrimaryButton("", ShootBullet);
            // Required: Start the countdown screen.
            CountdownScreen.instance.gameObject.SetActive(true);
        }
        
        // Handle any other room properties that changed. New properties can be defined as per requirements.
        // These properties can work as events and signals to communicate if anything changed at any player's 
        // end and needs to be addressed by other players in the room.
        foreach (var prop in propertiesThatChanged)
        {   
  
            // Create new assets if they dont exist on any user's end, 
            // otherwise update state (health, damage, points, isDestroyed) for existing assets. 
            if (((string)prop.Key).Contains("asset_") && prop.Value != null)
            {
                var temp = new PhotonSmartObjectDetails();
                JsonUtility.FromJsonOverwrite(prop.Value.ToString(), temp);
                if (!smartObjects.ContainsKey((string)prop.Key))
                {
                    if (!String.IsNullOrEmpty(temp.asset) && !String.IsNullOrEmpty(temp.bundle)) CreateAssetGame(temp);
                }
                else
                {   
                    // Update local state for a smartObject using "onRoomPropertiesUpdate" method
                    smartObjects[(string)prop.Key].onRoomPropertiesUpdate(temp);
                }
            }
        }
    }
    
    // Game specific helper function to create asset for players other than the master client
    private void CreateAssetGame(PhotonSmartObjectDetails obj)
    {
        var assetPosition = new float3(obj.posx, obj.posy, obj.posz);
        AddEntity((short)obj.id, obj.bundle, obj.asset, assetPosition, new float3(1, 1, 1), false);
    }
    
    // Provided as callback for primary button onclick.
    // In this case we would like to shoot bullets when primary button is clicked
    public void ShootBullet()
    {
        if (bulletAsset == "" || bulletBundle == "") return;
        ShootingManager.instance.Shoot(bulletAsset, bulletBundle);
    }
    
    // Unequip gun and anf switch to TPP view once the timer runs out
    // and game assets are cleared
    public override void GameClearObject()
    {
        UnequipInventory();
        GameManager.instance.SwitchToTppView();
    }
    
    // Equip any assets (ISO) for the player.
    // Since this is a shooter game, we equip a gun.
    public override void EquipInventory()
    {
        base.EquipInventory();
        GameplayUIManager.instance.OnSendEquipGun();
    }

    public override void UnequipInventory()
    {
        base.UnequipInventory();
        GameplayUIManager.instance.OnSendUnequipGun();
    }
}
```

  

Contact us
----------

You can contact us for any queries at [tanmay@doss.games](mailto:tanmay@doss.games) / [vishal@doss.games](mailto:vishal@doss.games) / [aniket@doss.games](mailto:aniket@doss.games)
