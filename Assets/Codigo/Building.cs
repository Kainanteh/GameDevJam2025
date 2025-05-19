using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum BuildingType
{
    Headquarters,
    FuerteNeutral
}

public class Building
{
    public string buildingName;
    public BuildingType type;
    public int ownerId = -1;

    public List<CellData> occupiedCells = new List<CellData>();

    public Building(string name, BuildingType type)
    {
        this.buildingName = name;
        this.type = type;
    }

    public bool isHeadquarters => type == BuildingType.Headquarters || type == BuildingType.FuerteNeutral;

    public virtual void PrintInfo()
    {
        Debug.Log($"Edificio: {buildingName}, Owner: {ownerId}");
    }
}
