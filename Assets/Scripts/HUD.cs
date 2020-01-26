using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    [SerializeField] private GameObject Heart;
    [SerializeField] private Transform Health;
    [SerializeField] private TextMeshProUGUI APCost;
    [SerializeField] private TextMeshProUGUI APAvailibility;
    [SerializeField] private TextMeshProUGUI Score;
    public TextMeshProUGUI NextWave;

    public List<GameObject> Hearts;

    public void UpdateHealth(int add)
    {
        if (add > 0)
        {
            for (int i = 0; i < add; i++)
            {
                GameObject temp = Instantiate(Heart, Health.position, Quaternion.identity, Health);
                temp.transform.localScale = new Vector3(10, 10, 10);
                temp.transform.localPosition = new Vector3(i * 50, 0, 0);
                temp.transform.localRotation = new Quaternion(0, 1, 0, 1);
                Hearts.Add(temp);
            }
        }
        else
        {
            if (-add > Hearts.Count)
            {
                for (int i = 0; i < Hearts.Count; i++)
                {
                    Destroy(Hearts[(Hearts.Count - i) - 1].gameObject);
                    Hearts.Remove(Hearts[Hearts.Count]);
                }
            }
            else
            {
                for (int i = 0; i > add; i--)
                {
                    Destroy(Hearts[(Hearts.Count + i) - 1].gameObject);
                    Hearts.Remove(Hearts[Hearts.Count]);
                }
            }
        }
    }

    public void UpdateAPCost(int Cost)
    {
        APCost.text = "AP Cost - " + Cost;
    }

    public void UpdateAPAvailibilty(int AP)
    {
        APAvailibility.text = "AP Availiable - " + AP;
    }

    public void UpdateScore(int score)
    {
        Score.text = "Score: " + score;
    }

    public void UpdateNextWave(int time)
    {
        NextWave.text = "Next Wave in " + time;
    }
}
