using Protocols;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataHandler
{
    private static DataHandler instance = null;

    public System.Action<PackIn> OnConnectCallback;
    public System.Action<PackIn> OnDisconnectCallback;

    public System.Action<string,List<PLCData>> OnGetPlcDataCallback;

    public System.Action<string,Dictionary<string,string>> OnGetPlcStatusCallback;

    public System.Action<List<WarningData>> OnQueryHisWarningDataCallback;
    
    public System.Action<QueryHistoryDataDTO> OnQueryHisDataCallback;

    public System.Action<StaticConfig> OnGetOtherPlcConfigCallback;
    public System.Action<List<WarningData>> OnGetWarningDataListCallback;
    public System.Action<AllConnectStatusDTO> OnGetConnectStatus;

    public static DataHandler getInstance
    {
        get
        {
            if (instance == null)
                instance = new DataHandler();
            return instance;
        }
    }
    public void Execute(SocketModel model)
    {
        //查询数据
        if (model.command == DataProtocol.QUERY)
        {
            if (model.message == null || model.message == string.Empty)
                return;

            MessageData msgData = LitJson.JsonMapper.ToObject<MessageData>(model.message);

            var list = LitJson.JsonMapper.ToObject<List<CustomerInformation>>(msgData.data);

            if (list.Count != 0)
            {

            }
            else
            {

            }
        }
        //搜索数据
        else if (model.command == DataProtocol.SEARCH)
        {
            var packin = LitJson.JsonMapper.ToObject<PackIn>(model.message);

            var list = LitJson.JsonMapper.ToObject<List<CustomerInformation>>(packin.message);

        }
        //更数据数据
        else if (model.command == DataProtocol.UPDATE)
        {
            var packin = LitJson.JsonMapper.ToObject<PackIn>(model.message);

            if (packin.code == CodeConstant.OK)
            {

            }
            else
            {

            }
        }
        //上传数据
        else if (model.command == DataProtocol.UPLOAD)
        {
            var packin = LitJson.JsonMapper.ToObject<PackIn>(model.message);

            if (packin.code == CodeConstant.OK)
            {

            }
            else
            {

            }
        }
        //删除数据
        else if (model.command == DataProtocol.DELETE)
        {
            var packin = LitJson.JsonMapper.ToObject<PackIn>(model.message);

            if (packin.code == CodeConstant.OK)
            {

            }
            else
            {

            }
        }
        else if (model.command == DataProtocol.PLC_CONNECT)
        {
            var packin = LitJson.JsonMapper.ToObject<PackIn>(model.message);

            Debug.Log(packin.message);

            OnConnectCallback?.Invoke(packin);

        }
        else if (model.command == DataProtocol.PLC_DISCONNECT)
        {
            var packin = LitJson.JsonMapper.ToObject<PackIn>(model.message);

            Debug.Log(packin.message);

            OnDisconnectCallback?.Invoke(packin);

        }
        else if (model.command == DataProtocol.GET_PLC_DATA)
        {
            var packin = LitJson.JsonMapper.ToObject<PackIn>(model.message);

            if (packin.code == CodeConstant.OK)
            {


                var dto = LitJson.JsonMapper.ToObject<GetPlcDataDTO>(packin.message);

                OnGetPlcDataCallback?.Invoke(dto.config, dto.plcDatas);
            }

        }
        else if (model.command == DataProtocol.PLC_CONNECT_STATUS)
        {
            var packin = LitJson.JsonMapper.ToObject<PackIn>(model.message);

            if (packin.code == CodeConstant.OK)
            {
                var dto = LitJson.JsonMapper.ToObject<PlcStatusDTO>(packin.message);

                OnGetPlcStatusCallback?.Invoke(dto.configName, dto.connectStatus);
            }
        }
        else if (model.command == DataProtocol.QUERY_HISOTRY_WARNING)
        {
            var packin = LitJson.JsonMapper.ToObject<PackIn>(model.message);

            var data = LitJson.JsonMapper.ToObject<QueryHistoryWarningDTO>(packin.message);

            if (OnQueryHisWarningDataCallback != null)
                OnQueryHisWarningDataCallback(data.results);
        }
        else if (model.command == DataProtocol.QUERY_HISOTRY_DATA)
        {
            var packin = LitJson.JsonMapper.ToObject<PackIn>(model.message);

            var data = LitJson.JsonMapper.ToObject<QueryHistoryDataDTO>(packin.message);

            if (OnQueryHisDataCallback != null)
                OnQueryHisDataCallback(data);
        }
        else if (model.command == DataProtocol.GET_OTHER_PLC_CONFIG)
        {
            var packin = LitJson.JsonMapper.ToObject<PackIn>(model.message);


            var data = LitJson.JsonMapper.ToObject<StaticConfig>(packin.message);

            if (data != null)
                GlobalInfo.m_StaticConfig = data;

            if (OnGetOtherPlcConfigCallback != null)
                OnGetOtherPlcConfigCallback(data);
        }
        else if (model.command == DataProtocol.Get_WARNINGDAT_LIST)
        {

            var packin = LitJson.JsonMapper.ToObject<PackIn>(model.message);


            var data = LitJson.JsonMapper.ToObject<List<WarningData>>(packin.message);

            if (OnGetWarningDataListCallback != null)
                OnGetWarningDataListCallback(data);

        }
        else if (model.command == DataProtocol.GET_CONNECT_ALL_STATUS)
        {
            var packin = LitJson.JsonMapper.ToObject<PackIn>(model.message);


            var data = LitJson.JsonMapper.ToObject<AllConnectStatusDTO>(packin.message);

            if (OnGetConnectStatus != null)
                OnGetConnectStatus(data);
        }
    }
}
