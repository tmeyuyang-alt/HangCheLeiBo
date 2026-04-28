using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace GameServer
{
    /// <summary>
    /// 定时任务
    /// </summary>
    public class TimerManager
    {

        private static TimerManager instance = null;
         public static TimerManager Instance
         {
            get
            {
                if (instance == null)
                    instance = new TimerManager();
                return instance;
            }
         }
       
        private List<TimerModel> timerModels = new List<TimerModel>();

        public TimerManager(){}

        public void UpdateTime()
        {

            //判断时间是否到
            for (int i = 0; i < timerModels.Count; )
            {
                if (timerModels[i] != null)
                {
                    if (timerModels[i].Time <= DateTime.Now.Ticks)
                    {
                        timerModels[i].Run();
                        timerModels.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    timerModels.RemoveAt(i);
                }
            }

        }

        /// <summary>
        /// 指触发时间
        /// </summary>
        public void AddTimeEvent(DateTime datetime, Action<object> callback)
        {
            long delayTime = datetime.Ticks - DateTime.Now.Ticks;

            if (delayTime <= 0)
                return;

            AddTimeEvent(delayTime, callback);
        }

        /// <summary>
        /// 指定延迟时间
        /// </summary>
        /// <param name="delayTime"></param>
        /// <param name="timeDelegate"></param>
        public void AddTimeEvent(long delayTime, Action<object> callback)
        {
            TimerModel timer = new TimerModel(DateTime.Now.Ticks + delayTime);

            timer.callback += callback;

            timerModels.Add(timer);
        }


    }
}
