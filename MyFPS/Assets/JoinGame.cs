using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;


public class JoinGame : MonoBehaviour {

	List<GameObject> roomList = new List<GameObject>();

	[SerializeField]
	private Text status;

	[SerializeField]
	private GameObject roomListItemPrefab;

	[SerializeField]
	private Transform roomListParent;

	private NetworkManager networkManager;

	void Start ()
	{
		networkManager = NetworkManager.singleton;
		if (networkManager.matchMaker == null)
		{
			networkManager.StartMatchMaker();
		}

		RefreshRoomList();
	}

	public void RefreshRoomList ()
	{
		ClearRoomList();

        if (networkManager.matchMaker == null)
        {
            //restarting matchmaker incase refreshing messes it up after
            //joining a game that was ended from its host
            networkManager.StartMatchMaker();
        }
		networkManager.matchMaker.ListMatches(0, 20, "", true, 0, 0, OnMatchList);
		status.text = "Loading...";
	}

	public void OnMatchList (bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
	{
		status.text = "";

		if (!success || matchList == null)
		{
			status.text = "Couldn't get room list.";
			return;
		}

		foreach (MatchInfoSnapshot match in matchList)
		{
			GameObject _roomListItemGO = Instantiate(roomListItemPrefab);
			_roomListItemGO.transform.SetParent(roomListParent);

			RoomListItem _roomListItem = _roomListItemGO.GetComponent<RoomListItem>();
			if (_roomListItem != null)
			{
				_roomListItem.Setup(match, JoinRoom);
			}

			
			// as well as setting up a callback function that will join the game.

			roomList.Add(_roomListItemGO);
		}

		if (roomList.Count == 0)
		{
			status.text = "No rooms at the moment.";
		}
	}

	void ClearRoomList()
	{
		for (int i = 0; i < roomList.Count; i++)
		{
			Destroy(roomList[i]);
		}

		roomList.Clear();
	}

	public void JoinRoom (MatchInfoSnapshot _match)
	{
		networkManager.matchMaker.JoinMatch(_match.networkId, "", "", "", 0, 0, networkManager.OnMatchJoined);
        StartCoroutine(WaitForJoin());
	}

    IEnumerator WaitForJoin()
    {
        ClearRoomList();

        //while this countdown is not 0, the game will attempt to connect
        int countdown = 10;
        while (countdown > 0)
        {
            status.text = "JOINING...(" + countdown + ")";
            yield return new WaitForSeconds(1);
            countdown--;
        }
        //failed to connect to the game
        status.text = "failed to join game";
        yield return new WaitForSeconds(1);
        //gets information for our match incase the room is still being hosted by unitys matchmaker
        //so this makes sure to drop the connection
        MatchInfo matchInfo = networkManager.matchInfo;
        if (matchInfo != null)
        {
            //assures quit hosting of game
            networkManager.matchMaker.DropConnection(matchInfo.networkId, matchInfo.nodeId, 0, networkManager.OnDropConnection);
            networkManager.StopHost();
        }
        RefreshRoomList();

    }

}
