using System.Collections;
using System.Collections.Generic;
using UnityEngine;


class CardSetDataRootObject
{
    public bool has_more;
    public string next_page;
    public CardData[] data;
}

public class CardSetData
{
    public LinkedList<CardData> cards;
    public string name;
    public string search_uri;
    public string block;
    public string code;
    public string set_type;
}

public class CardData
{
    public string name;
    public string mana_cost;
    public string cmc;
    public string type_line;
    public ImageURIS image_uris;
    public string[] color_identity;
    public string set;
    public string flavor_text;
    public string rarity;
    public bool booster;
    public string power;
    public string toughness;
    public string oracle_text;
    public string loyalty;
}

public class ImageURIS
{
    public string small;
    public string normal;
    public string large;
    public string png;
    public string art_crop;
    public string border_crop;

}

