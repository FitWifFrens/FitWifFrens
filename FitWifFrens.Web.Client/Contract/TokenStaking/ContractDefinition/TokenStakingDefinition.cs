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

    public partial class DistributeResultsBeginNextCycleFunction : DistributeResultsBeginNextCycleFunctionBase { }

    [Function("DistributeResultsBeginNextCycle")]
    public class DistributeResultsBeginNextCycleFunctionBase : FunctionMessage
    {

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

    public partial class AllowWithdrawFunction : AllowWithdrawFunctionBase { }

    [Function("allowWithdraw", "bool")]
    public class AllowWithdrawFunctionBase : FunctionMessage
    {

    }

    public partial class ChallengerCountFunction : ChallengerCountFunctionBase { }

    [Function("challengerCount", "uint256")]
    public class ChallengerCountFunctionBase : FunctionMessage
    {

    }

    public partial class CheckResultsFunction : CheckResultsFunctionBase { }

    [Function("checkResults")]
    public class CheckResultsFunctionBase : FunctionMessage
    {

    }

    public partial class CreatePledgeFunction : CreatePledgeFunctionBase { }

    [Function("createPledge")]
    public class CreatePledgeFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "amount", 1)]
        public virtual BigInteger Amount { get; set; }
        [Parameter("string", "uniqueID", 2)]
        public virtual string UniqueID { get; set; }
    }

    public partial class CycleFunction : CycleFunctionBase { }

    [Function("cycle", "uint256")]
    public class CycleFunctionBase : FunctionMessage
    {

    }

    public partial class DisableWithdrawFunction : DisableWithdrawFunctionBase { }

    [Function("disableWithdraw")]
    public class DisableWithdrawFunctionBase : FunctionMessage
    {

    }

    public partial class EnableWithdrawFunction : EnableWithdrawFunctionBase { }

    [Function("enableWithdraw")]
    public class EnableWithdrawFunctionBase : FunctionMessage
    {

    }

    public partial class HasStakedFunction : HasStakedFunctionBase { }

    [Function("hasStaked", "bool")]
    public class HasStakedFunctionBase : FunctionMessage
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
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

    public partial class ParticipantsFunction : ParticipantsFunctionBase { }

    [Function("participants", "address")]
    public class ParticipantsFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class PendingResultsFunction : PendingResultsFunctionBase { }

    [Function("pendingResults", "bool")]
    public class PendingResultsFunctionBase : FunctionMessage
    {

    }

    public partial class RenounceOwnershipFunction : RenounceOwnershipFunctionBase { }

    [Function("renounceOwnership")]
    public class RenounceOwnershipFunctionBase : FunctionMessage
    {

    }

    public partial class ResultThisCycleFunction : ResultThisCycleFunctionBase { }

    [Function("resultThisCycle", "bool")]
    public class ResultThisCycleFunctionBase : FunctionMessage
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class RewardTokensFunction : RewardTokensFunctionBase { }

    [Function("rewardTokens", "uint256")]
    public class RewardTokensFunctionBase : FunctionMessage
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class RewardsPoolFunction : RewardsPoolFunctionBase { }

    [Function("rewardsPool", "uint256")]
    public class RewardsPoolFunctionBase : FunctionMessage
    {

    }

    public partial class SetResultFunction : SetResultFunctionBase { }

    [Function("setResult")]
    public class SetResultFunctionBase : FunctionMessage
    {
        [Parameter("address", "_userAddress", 1)]
        public virtual string UserAddress { get; set; }
        [Parameter("bool", "resultstate", 2)]
        public virtual bool Resultstate { get; set; }
    }

    public partial class StakeCompleteFunction : StakeCompleteFunctionBase { }

    [Function("stakeComplete", "bool")]
    public class StakeCompleteFunctionBase : FunctionMessage
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
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

    public partial class UniqueIDUsedFunction : UniqueIDUsedFunctionBase { }

    [Function("uniqueIDUsed", "bool")]
    public class UniqueIDUsedFunctionBase : FunctionMessage
    {
        [Parameter("string", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class UniqueIDmapFunction : UniqueIDmapFunctionBase { }

    [Function("uniqueIDmap", "string")]
    public class UniqueIDmapFunctionBase : FunctionMessage
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class WithdrawStakeFunction : WithdrawStakeFunctionBase { }

    [Function("withdrawStake")]
    public class WithdrawStakeFunctionBase : FunctionMessage
    {

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

    public partial class AllowWithdrawOutputDTO : AllowWithdrawOutputDTOBase { }

    [FunctionOutput]
    public class AllowWithdrawOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }

    public partial class ChallengerCountOutputDTO : ChallengerCountOutputDTOBase { }

    [FunctionOutput]
    public class ChallengerCountOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }





    public partial class CycleOutputDTO : CycleOutputDTOBase { }

    [FunctionOutput]
    public class CycleOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }





    public partial class HasStakedOutputDTO : HasStakedOutputDTOBase { }

    [FunctionOutput]
    public class HasStakedOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
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

    public partial class ParticipantsOutputDTO : ParticipantsOutputDTOBase { }

    [FunctionOutput]
    public class ParticipantsOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class PendingResultsOutputDTO : PendingResultsOutputDTOBase { }

    [FunctionOutput]
    public class PendingResultsOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }



    public partial class ResultThisCycleOutputDTO : ResultThisCycleOutputDTOBase { }

    [FunctionOutput]
    public class ResultThisCycleOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }

    public partial class RewardTokensOutputDTO : RewardTokensOutputDTOBase { }

    [FunctionOutput]
    public class RewardTokensOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class RewardsPoolOutputDTO : RewardsPoolOutputDTOBase { }

    [FunctionOutput]
    public class RewardsPoolOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }



    public partial class StakeCompleteOutputDTO : StakeCompleteOutputDTOBase { }

    [FunctionOutput]
    public class StakeCompleteOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
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



    public partial class UniqueIDUsedOutputDTO : UniqueIDUsedOutputDTOBase { }

    [FunctionOutput]
    public class UniqueIDUsedOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }

    public partial class UniqueIDmapOutputDTO : UniqueIDmapOutputDTOBase { }

    [FunctionOutput]
    public class UniqueIDmapOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("string", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }


}
