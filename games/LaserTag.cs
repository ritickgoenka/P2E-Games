// MIT License

// Copyright (c) 2022 keshavtiwari

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

public class LaserTag : MiniGameBase
{
    List<Tuple<string, string>> enemies = new List<Tuple<string, string>>();

    string laserAsset = "";
    string laserBundle = "";

    public LaserTag(Dictionary<string, object> gameAssetInput) : base(gameAssetInput)
    {
        foreach (var gameAsset in gameAssetInput)
        {
            if (gameAsset.Key.Contains("enemies")) enemies.Add((Tuple<string, string>)(gameAsset.Value));
            if (gameAsset.Key.Contains("laser"))
            {
                var t = (Tuple<string, string>)(gameAsset.Value);
                laserAsset = t.Item2;
                laserBundle = t.Item1;

                smartObjects["asset_100001"] = AssetRegistry.GetSmartObjectFromData(laserAsset, new SmartObjectData { id = 100001, sendingProperties = 3, damageSend = 10, receivingProperties = (int)MiniGameUtils.Properties.POINT });
            }
        }
    }

    public void ResetPlayerProperties()
    {
        GameManager.instance.SwitchToTppView();
        if (entityManager.HasComponent<SmartObjectData>(GameManager.instance.playerEntity))
        {
            entityManager.SetComponentData<SmartObjectData>(GameManager.instance.playerEntity, new SmartObjectData { id = 10000 });
        }
        else
        {
            entityManager.AddComponentData(GameManager.instance.playerEntity, new SmartObjectData { id = 10000 });
        }

        smartObjects["asset_10000"] = AssetRegistry.GetSmartObject("player", GameManager.instance.playerEntity);

        Player player = PhotonNetwork.LocalPlayer;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "score", 0 } });
    }

    public override void PlaceAssets()
    {
        var assetHashtable = new ExitGames.Client.Photon.Hashtable { };
        List<Vector3> assetPlaceholders = PlaceholderManager.instance.Placeholders.GetRandomItems(100);
        short id = 0;
        foreach (Vector3 assetPosition in assetPlaceholders)
        {
            if (enemies.Count > 0)
            {
                var enemyTup = enemies[UnityEngine.Random.Range(0, enemies.Count)];
                var temp = AddEntity(id, enemyTup.Item1, enemyTup.Item2, assetPosition, new float3(1, 1, 1), false);
                id++;
                assetHashtable.Add(temp.key, JsonUtility.ToJson(new PhotonSmartObjectDetails(smartObjects[temp.key], enemyTup.Item2, enemyTup.Item1, assetPosition, temp.rotation)));
            }
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(assetHashtable);

    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.TryGetValue("start_game", out object status))
        {
            ResetPlayerProperties();
            EquipInventory();
            EnablePrimaryButton("", ShootLaser);
            CountdownScreen.instance.gameObject.SetActive(true);
        }

        foreach (var prop in propertiesThatChanged)
        {
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
                    smartObjects[(string)prop.Key].onRoomPropertiesUpdate(temp);
                }
            }
        }
    }

    private void CreateAssetGame(PhotonSmartObjectDetails obj)
    {
        var assetPosition = new float3(obj.posx, obj.posy, obj.posz);
        AddEntity((short)obj.id, obj.bundle, obj.asset, assetPosition, new float3(1, 1, 1), false);
    }

    public void ShootLaser()
    {
        if (laserAsset == "" || laserBundle == "") return;
        ShootManager.instance.Shoot(laserAsset, laserBundle);
    }

    public override void GameClearObject()
    {
        UnequipInventory();
        GameManager.instance.SwitchToTppView();
    }

    public override void EquipInventory()
    {
        base.EquipInventory();
        GameplayUIManager.instance.OnSendEquipLaserGun();
    }

    public override void UnequipInventory()
    {
        base.UnequipInventory();
        GameplayUIManager.instance.OnSendUnequipLaserGun();
    }
}
