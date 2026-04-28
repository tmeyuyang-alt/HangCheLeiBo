using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;

[RequireComponent(typeof(LineChart))]
public class LineChartNameConfig : MonoBehaviour
{
   public LineChart lineChart;

   private void Start()
   {
      lineChart=GetComponent<LineChart>();
      foreach (var VARIABLE in lineChart.series)
      {
         VARIABLE.data[0].name = NameConfig.Instance.LinKuang;
         VARIABLE.data[1].name = NameConfig.Instance.GuiShi;
         VARIABLE.data[2].name = NameConfig.Instance.LengYaQiu;
         VARIABLE.data[3].name = NameConfig.Instance.Mei;
         VARIABLE.data[3].name = NameConfig.Instance.ShaoJieQiu;
      }
      
      
      
   }
}
