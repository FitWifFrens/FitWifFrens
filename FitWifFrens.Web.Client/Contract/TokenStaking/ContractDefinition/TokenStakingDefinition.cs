using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Contracts.CQS;
using Nethereum.Contracts;
using System.Threading;

namespace FitWifFrens.Web.Client.Contract.TokenStaking.ContractDefinition
{


    public partial class TokenStakingDeployment : TokenStakingDeploymentBase
    {
        public TokenStakingDeployment() : base(BYTECODE) { }
        public TokenStakingDeployment(string byteCode) : base(byteCode) { }
    }

    public class TokenStakingDeploymentBase : ContractDeploymentMessage
    {
        public static string BYTECODE = "";
        public TokenStakingDeploymentBase() : base(BYTECODE) { }
        public TokenStakingDeploymentBase(string byteCode) : base(byteCode) { }
        [Parameter("address", "_tokenAddress", 1)]
        public virtual string TokenAddress { get; set; }
        [Parameter("uint256", "_activityThreshold", 2)]
        public virtual BigInteger ActivityThreshold { get; set; }
        [Parameter("uint256", "_minuteThreshold", 3)]
        public virtual BigInteger MinuteThreshold { get; set; }
    }

    public partial class TokenAddressFunction : TokenAddressFunctionBase { }

    [Function("TokenAddress", "address")]
    public class TokenAddressFunctionBase : FunctionMessage
    {

    }

    public partial class ActivityThresholdFunction : ActivityThresholdFunctionBase { }

    [Function("activityThreshold", "uint256")]
    public class ActivityThresholdFunctionBase : FunctionMessage
    {

    }

    public partial class CheckResultsFunction : CheckResultsFunctionBase { }

    [Function("checkResults")]
    public class CheckResultsFunctionBase : FunctionMessage
    {

    }

    public partial class DistributeUSDCFunction : DistributeUSDCFunctionBase { }

    [Function("distributeUSDC")]
    public class DistributeUSDCFunctionBase : FunctionMessage
    {
        [Parameter("address[]", "participants", 1)]
        public virtual List<string> Participants { get; set; }
        [Parameter("uint256[]", "amounts", 2)]
        public virtual List<BigInteger> Amounts { get; set; }
    }

    public partial class MinuteThresholdFunction : MinuteThresholdFunctionBase { }

    [Function("minuteThreshold", "uint256")]
    public class MinuteThresholdFunctionBase : FunctionMessage
    {

    }

    public partial class OwnerFunction : OwnerFunctionBase { }

    [Function("owner", "address")]
    public class OwnerFunctionBase : FunctionMessage
    {

    }

    public partial class RenounceOwnershipFunction : RenounceOwnershipFunctionBase { }

    [Function("renounceOwnership")]
    public class RenounceOwnershipFunctionBase : FunctionMessage
    {

    }

    public partial class StakeTokenFunction : StakeTokenFunctionBase { }

    [Function("stakeToken")]
    public class StakeTokenFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "amount", 1)]
        public virtual BigInteger Amount { get; set; }
    }

    public partial class StakedTokensFunction : StakedTokensFunctionBase { }

    [Function("stakedTokens", "uint256")]
    public class StakedTokensFunctionBase : FunctionMessage
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class TotalStakedFunction : TotalStakedFunctionBase { }

    [Function("totalStaked", "uint256")]
    public class TotalStakedFunctionBase : FunctionMessage
    {

    }

    public partial class TransferOwnershipFunction : TransferOwnershipFunctionBase { }

    [Function("transferOwnership")]
    public class TransferOwnershipFunctionBase : FunctionMessage
    {
        [Parameter("address", "newOwner", 1)]
        public virtual string NewOwner { get; set; }
    }

    public partial class WithdrawStakeFunction : WithdrawStakeFunctionBase { }

    [Function("withdrawStake")]
    public class WithdrawStakeFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "amount", 1)]
        public virtual BigInteger Amount { get; set; }
    }

    public partial class OwnershipTransferredEventDTO : OwnershipTransferredEventDTOBase { }

    [Event("OwnershipTransferred")]
    public class OwnershipTransferredEventDTOBase : IEventDTO
    {
        [Parameter("address", "previousOwner", 1, true )]
        public virtual string PreviousOwner { get; set; }
        [Parameter("address", "newOwner", 2, true )]
        public virtual string NewOwner { get; set; }
    }

    public partial class OwnableInvalidOwnerError : OwnableInvalidOwnerErrorBase { }

    [Error("OwnableInvalidOwner")]
    public class OwnableInvalidOwnerErrorBase : IErrorDTO
    {
        [Parameter("address", "owner", 1)]
        public virtual string Owner { get; set; }
    }

    public partial class OwnableUnauthorizedAccountError : OwnableUnauthorizedAccountErrorBase { }

    [Error("OwnableUnauthorizedAccount")]
    public class OwnableUnauthorizedAccountErrorBase : IErrorDTO
    {
        [Parameter("address", "account", 1)]
        public virtual string Account { get; set; }
    }

    public partial class TokenAddressOutputDTO : TokenAddressOutputDTOBase { }

    [FunctionOutput]
    public class TokenAddressOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class ActivityThresholdOutputDTO : ActivityThresholdOutputDTOBase { }

    [FunctionOutput]
    public class ActivityThresholdOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }





    public partial class MinuteThresholdOutputDTO : MinuteThresholdOutputDTOBase { }

    [FunctionOutput]
    public class MinuteThresholdOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class OwnerOutputDTO : OwnerOutputDTOBase { }

    [FunctionOutput]
    public class OwnerOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }





    public partial class StakedTokensOutputDTO : StakedTokensOutputDTOBase { }

    [FunctionOutput]
    public class StakedTokensOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class TotalStakedOutputDTO : TotalStakedOutputDTOBase { }

    [FunctionOutput]
    public class TotalStakedOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }




}
