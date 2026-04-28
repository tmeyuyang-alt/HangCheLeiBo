using System;

namespace Protocols
{
    /// <summary>
    /// 登录模块
    /// </summary>
    public class LoginProtocol
    {
        public const int LOGIN = 1;

        public const int REGISTRY = 2;

        public const int Error = 0;
    }

    public class LoginCommandProtocol
    {
        /// <summary>
        /// 请求公钥
        /// </summary>
        public const int REQ_PK = 1;
        /// <summary>
        /// 登录操作
        /// </summary>
        public const int LOGIN = 2;

        /// <summary>
        /// 重置密码
        /// </summary>
        public const int REST_PSW = 3;


        public const int CREATE_ACCOUNT = 4;

        public const int MODIFIY_ACCOUNT = 5;

    }
}
