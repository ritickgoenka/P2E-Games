/*MIT License

Copyright (c) 2022 Doss
Copyright (c) 2022 05Tushar

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.*/

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

public class CollectTheCandies : MiniGameBase
{
    List<Tuple<string, string>> candies = new List<Tuple<string, string>>();
    List<Tuple<string, string>> superCandies = new List<Tuple<string, string>>();

    public CollectTheCandies(Dictionary<string, object> gameAssetInput) : base(gameAssetInput)
    {
        foreach (var gameAsset in gameAssetInput)
        {
            if (gameAsset.Key.Contains("candy")) candies.Add((Tuple<string, string>)(gameAsset.Value));
            if (gameAsset.Key.Contains("superCandy")) superCandies.Add((Tuple<string, string>)(gameAsset.Value));
        }
    }

    public void ResetPlayerProperties()
    {
        if (entityManager.HasComponent<SmartObjectData>(GameManager.instance.playerEntity))
        {

            entityManager.SetComponentData<SmartObjectData>(GameManager.instance.playerEntity, new SmartObjectData { id = 10000, receivingProperties = 1234 });
        }
        else
        {

            entityManager.AddComponentData(GameManager.instance.playerEntity, new SmartObjectData { id = 10000, receivingProperties = 1234 });
        }

        smartObjects["asset_10000"] = AssetRegistry.GetSmartObject("player", GameManager.instance.playerEntity);

        Player player = PhotonNetwork.LocalPlayer;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "score", 0 } });
    }

    public override void PlaceAssets()
    {
        //
        var assetHashtable = new ExitGames.Client.Photon.Hashtable { };
        //
        List<Vector3> candiesPlaceholders = PlaceholderManager.instance.Placeholders.GetRandomItems(20);
        short id = 0;
        foreach (Vector3 candyPosition in candiesPlaceholders)
        {
            if (candies.Count > 0)
            {
                var candyTup = candies[UnityEngine.Random.Range(0, candies.Count)];
                var temp1 = AddEntity(id, candyTup.Item1, candyTup.Item2, candyPosition, new float3(1, 1, 1), false);
                assetHashtable.Add(temp1.key, JsonUtility.ToJson(new PhotonSmartObjectDetails(smartObjects[temp1.key], candyTup.Item2, candyTup.Item1, candyPosition, temp1.rotation)));
            }

            if (superCandies.Count > 0)
            {
                var superCandyTup = superCandies[UnityEngine.Random.Range(0, superCandies.Count)];
                var temp1 = AddEntity(id, superCandyTup.Item1, superCandyTup.Item2, candyPosition, new float3(1, 1, 1), false);
                assetHashtable.Add(temp1.key, JsonUtility.ToJson(new PhotonSmartObjectDetails(smartObjects[temp1.key], superCandyTup.Item2, superCandyTup.Item1, candyPosition, temp1.rotation)));
            }

        }
        PhotonNetwork.CurrentRoom.SetCustomProperties(assetHashtable);

    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.TryGetValue("start_game", out object status))
        {
            ResetPlayerProperties();
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
        var candyPosition = new float3(obj.posx, obj.posy, obj.posz);
        AddEntity((short)obj.id, obj.bundle, obj.asset, candyPosition, new float3(1, 1, 1), false);
    }
}
