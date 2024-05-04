using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Web3;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Contracts.CQS;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Contracts;
using System.Threading;
using FitWifFrens.Web.Client.Contract.TokenStaking.ContractDefinition;

namespace FitWifFrens.Web.Client.Contract.TokenStaking
{
    public partial class TokenStakingService: ContractWeb3ServiceBase
    {
        public static Task<TransactionReceipt> DeployContractAndWaitForReceiptAsync(Nethereum.Web3.IWeb3 web3, TokenStakingDeployment tokenStakingDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            return web3.Eth.GetContractDeploymentHandler<TokenStakingDeployment>().SendRequestAndWaitForReceiptAsync(tokenStakingDeployment, cancellationTokenSource);
        }

        public static Task<string> DeployContractAsync(Nethereum.Web3.IWeb3 web3, TokenStakingDeployment tokenStakingDeployment)
        {
            return web3.Eth.GetContractDeploymentHandler<TokenStakingDeployment>().SendRequestAsync(tokenStakingDeployment);
        }

        public static async Task<TokenStakingService> DeployContractAndGetServiceAsync(Nethereum.Web3.IWeb3 web3, TokenStakingDeployment tokenStakingDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            var receipt = await DeployContractAndWaitForReceiptAsync(web3, tokenStakingDeployment, cancellationTokenSource);
            return new TokenStakingService(web3, receipt.ContractAddress);
        }

        public TokenStakingService(Nethereum.Web3.IWeb3 web3, string contractAddress) : base(web3, contractAddress)
        {
        }

        public Task<string> DistributeResultsBeginNextCycleRequestAsync(DistributeResultsBeginNextCycleFunction distributeResultsBeginNextCycleFunction)
        {
             return ContractHandler.SendRequestAsync(distributeResultsBeginNextCycleFunction);
        }

        public Task<string> DistributeResultsBeginNextCycleRequestAsync()
        {
             return ContractHandler.SendRequestAsync<DistributeResultsBeginNextCycleFunction>();
        }

        public Task<TransactionReceipt> DistributeResultsBeginNextCycleRequestAndWaitForReceiptAsync(DistributeResultsBeginNextCycleFunction distributeResultsBeginNextCycleFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(distributeResultsBeginNextCycleFunction, cancellationToken);
        }

        public Task<TransactionReceipt> DistributeResultsBeginNextCycleRequestAndWaitForReceiptAsync(CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync<DistributeResultsBeginNextCycleFunction>(null, cancellationToken);
        }

        public Task<string> TokenAddressQueryAsync(TokenAddressFunction tokenAddressFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TokenAddressFunction, string>(tokenAddressFunction, blockParameter);
        }

        
        public Task<string> TokenAddressQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TokenAddressFunction, string>(null, blockParameter);
        }

        public Task<BigInteger> ActivityThresholdQueryAsync(ActivityThresholdFunction activityThresholdFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ActivityThresholdFunction, BigInteger>(activityThresholdFunction, blockParameter);
        }

        
        public Task<BigInteger> ActivityThresholdQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ActivityThresholdFunction, BigInteger>(null, blockParameter);
        }

        public Task<bool> AllowWithdrawQueryAsync(AllowWithdrawFunction allowWithdrawFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<AllowWithdrawFunction, bool>(allowWithdrawFunction, blockParameter);
        }

        
        public Task<bool> AllowWithdrawQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<AllowWithdrawFunction, bool>(null, blockParameter);
        }

        public Task<BigInteger> ChallengerCountQueryAsync(ChallengerCountFunction challengerCountFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ChallengerCountFunction, BigInteger>(challengerCountFunction, blockParameter);
        }

        
        public Task<BigInteger> ChallengerCountQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ChallengerCountFunction, BigInteger>(null, blockParameter);
        }

        public Task<string> CheckResultsRequestAsync(CheckResultsFunction checkResultsFunction)
        {
             return ContractHandler.SendRequestAsync(checkResultsFunction);
        }

        public Task<string> CheckResultsRequestAsync()
        {
             return ContractHandler.SendRequestAsync<CheckResultsFunction>();
        }

        public Task<TransactionReceipt> CheckResultsRequestAndWaitForReceiptAsync(CheckResultsFunction checkResultsFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(checkResultsFunction, cancellationToken);
        }

        public Task<TransactionReceipt> CheckResultsRequestAndWaitForReceiptAsync(CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync<CheckResultsFunction>(null, cancellationToken);
        }

        public Task<string> CreatePledgeRequestAsync(CreatePledgeFunction createPledgeFunction)
        {
             return ContractHandler.SendRequestAsync(createPledgeFunction);
        }

        public Task<TransactionReceipt> CreatePledgeRequestAndWaitForReceiptAsync(CreatePledgeFunction createPledgeFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(createPledgeFunction, cancellationToken);
        }

        public Task<string> CreatePledgeRequestAsync(BigInteger amount, string uniqueID)
        {
            var createPledgeFunction = new CreatePledgeFunction();
                createPledgeFunction.Amount = amount;
                createPledgeFunction.UniqueID = uniqueID;
            
             return ContractHandler.SendRequestAsync(createPledgeFunction);
        }

        public Task<TransactionReceipt> CreatePledgeRequestAndWaitForReceiptAsync(BigInteger amount, string uniqueID, CancellationTokenSource cancellationToken = null)
        {
            var createPledgeFunction = new CreatePledgeFunction();
                createPledgeFunction.Amount = amount;
                createPledgeFunction.UniqueID = uniqueID;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(createPledgeFunction, cancellationToken);
        }

        public Task<BigInteger> CycleQueryAsync(CycleFunction cycleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<CycleFunction, BigInteger>(cycleFunction, blockParameter);
        }

        
        public Task<BigInteger> CycleQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<CycleFunction, BigInteger>(null, blockParameter);
        }

        public Task<string> DisableWithdrawRequestAsync(DisableWithdrawFunction disableWithdrawFunction)
        {
             return ContractHandler.SendRequestAsync(disableWithdrawFunction);
        }

        public Task<string> DisableWithdrawRequestAsync()
        {
             return ContractHandler.SendRequestAsync<DisableWithdrawFunction>();
        }

        public Task<TransactionReceipt> DisableWithdrawRequestAndWaitForReceiptAsync(DisableWithdrawFunction disableWithdrawFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(disableWithdrawFunction, cancellationToken);
        }

        public Task<TransactionReceipt> DisableWithdrawRequestAndWaitForReceiptAsync(CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync<DisableWithdrawFunction>(null, cancellationToken);
        }

        public Task<string> EnableWithdrawRequestAsync(EnableWithdrawFunction enableWithdrawFunction)
        {
             return ContractHandler.SendRequestAsync(enableWithdrawFunction);
        }

        public Task<string> EnableWithdrawRequestAsync()
        {
             return ContractHandler.SendRequestAsync<EnableWithdrawFunction>();
        }

        public Task<TransactionReceipt> EnableWithdrawRequestAndWaitForReceiptAsync(EnableWithdrawFunction enableWithdrawFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(enableWithdrawFunction, cancellationToken);
        }

        public Task<TransactionReceipt> EnableWithdrawRequestAndWaitForReceiptAsync(CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync<EnableWithdrawFunction>(null, cancellationToken);
        }

        public Task<bool> HasStakedQueryAsync(HasStakedFunction hasStakedFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<HasStakedFunction, bool>(hasStakedFunction, blockParameter);
        }

        
        public Task<bool> HasStakedQueryAsync(string returnValue1, BlockParameter blockParameter = null)
        {
            var hasStakedFunction = new HasStakedFunction();
                hasStakedFunction.ReturnValue1 = returnValue1;
            
            return ContractHandler.QueryAsync<HasStakedFunction, bool>(hasStakedFunction, blockParameter);
        }

        public Task<BigInteger> MinuteThresholdQueryAsync(MinuteThresholdFunction minuteThresholdFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<MinuteThresholdFunction, BigInteger>(minuteThresholdFunction, blockParameter);
        }

        
        public Task<BigInteger> MinuteThresholdQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<MinuteThresholdFunction, BigInteger>(null, blockParameter);
        }

        public Task<string> OwnerQueryAsync(OwnerFunction ownerFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<OwnerFunction, string>(ownerFunction, blockParameter);
        }

        
        public Task<string> OwnerQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<OwnerFunction, string>(null, blockParameter);
        }

        public Task<string> ParticipantsQueryAsync(ParticipantsFunction participantsFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ParticipantsFunction, string>(participantsFunction, blockParameter);
        }

        
        public Task<string> ParticipantsQueryAsync(BigInteger returnValue1, BlockParameter blockParameter = null)
        {
            var participantsFunction = new ParticipantsFunction();
                participantsFunction.ReturnValue1 = returnValue1;
            
            return ContractHandler.QueryAsync<ParticipantsFunction, string>(participantsFunction, blockParameter);
        }

        public Task<bool> PendingResultsQueryAsync(PendingResultsFunction pendingResultsFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<PendingResultsFunction, bool>(pendingResultsFunction, blockParameter);
        }

        
        public Task<bool> PendingResultsQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<PendingResultsFunction, bool>(null, blockParameter);
        }

        public Task<string> RenounceOwnershipRequestAsync(RenounceOwnershipFunction renounceOwnershipFunction)
        {
             return ContractHandler.SendRequestAsync(renounceOwnershipFunction);
        }

        public Task<string> RenounceOwnershipRequestAsync()
        {
             return ContractHandler.SendRequestAsync<RenounceOwnershipFunction>();
        }

        public Task<TransactionReceipt> RenounceOwnershipRequestAndWaitForReceiptAsync(RenounceOwnershipFunction renounceOwnershipFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(renounceOwnershipFunction, cancellationToken);
        }

        public Task<TransactionReceipt> RenounceOwnershipRequestAndWaitForReceiptAsync(CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync<RenounceOwnershipFunction>(null, cancellationToken);
        }

        public Task<bool> ResultThisCycleQueryAsync(ResultThisCycleFunction resultThisCycleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ResultThisCycleFunction, bool>(resultThisCycleFunction, blockParameter);
        }

        
        public Task<bool> ResultThisCycleQueryAsync(string returnValue1, BlockParameter blockParameter = null)
        {
            var resultThisCycleFunction = new ResultThisCycleFunction();
                resultThisCycleFunction.ReturnValue1 = returnValue1;
            
            return ContractHandler.QueryAsync<ResultThisCycleFunction, bool>(resultThisCycleFunction, blockParameter);
        }

        public Task<BigInteger> RewardTokensQueryAsync(RewardTokensFunction rewardTokensFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<RewardTokensFunction, BigInteger>(rewardTokensFunction, blockParameter);
        }

        
        public Task<BigInteger> RewardTokensQueryAsync(string returnValue1, BlockParameter blockParameter = null)
        {
            var rewardTokensFunction = new RewardTokensFunction();
                rewardTokensFunction.ReturnValue1 = returnValue1;
            
            return ContractHandler.QueryAsync<RewardTokensFunction, BigInteger>(rewardTokensFunction, blockParameter);
        }

        public Task<BigInteger> RewardsPoolQueryAsync(RewardsPoolFunction rewardsPoolFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<RewardsPoolFunction, BigInteger>(rewardsPoolFunction, blockParameter);
        }

        
        public Task<BigInteger> RewardsPoolQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<RewardsPoolFunction, BigInteger>(null, blockParameter);
        }

        public Task<string> SetResultRequestAsync(SetResultFunction setResultFunction)
        {
             return ContractHandler.SendRequestAsync(setResultFunction);
        }

        public Task<TransactionReceipt> SetResultRequestAndWaitForReceiptAsync(SetResultFunction setResultFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setResultFunction, cancellationToken);
        }

        public Task<string> SetResultRequestAsync(string userAddress, bool resultstate)
        {
            var setResultFunction = new SetResultFunction();
                setResultFunction.UserAddress = userAddress;
                setResultFunction.Resultstate = resultstate;
            
             return ContractHandler.SendRequestAsync(setResultFunction);
        }

        public Task<TransactionReceipt> SetResultRequestAndWaitForReceiptAsync(string userAddress, bool resultstate, CancellationTokenSource cancellationToken = null)
        {
            var setResultFunction = new SetResultFunction();
                setResultFunction.UserAddress = userAddress;
                setResultFunction.Resultstate = resultstate;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setResultFunction, cancellationToken);
        }

        public Task<bool> StakeCompleteQueryAsync(StakeCompleteFunction stakeCompleteFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<StakeCompleteFunction, bool>(stakeCompleteFunction, blockParameter);
        }

        
        public Task<bool> StakeCompleteQueryAsync(string returnValue1, BlockParameter blockParameter = null)
        {
            var stakeCompleteFunction = new StakeCompleteFunction();
                stakeCompleteFunction.ReturnValue1 = returnValue1;
            
            return ContractHandler.QueryAsync<StakeCompleteFunction, bool>(stakeCompleteFunction, blockParameter);
        }

        public Task<BigInteger> StakedTokensQueryAsync(StakedTokensFunction stakedTokensFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<StakedTokensFunction, BigInteger>(stakedTokensFunction, blockParameter);
        }

        
        public Task<BigInteger> StakedTokensQueryAsync(string returnValue1, BlockParameter blockParameter = null)
        {
            var stakedTokensFunction = new StakedTokensFunction();
                stakedTokensFunction.ReturnValue1 = returnValue1;
            
            return ContractHandler.QueryAsync<StakedTokensFunction, BigInteger>(stakedTokensFunction, blockParameter);
        }

        public Task<BigInteger> TotalStakedQueryAsync(TotalStakedFunction totalStakedFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TotalStakedFunction, BigInteger>(totalStakedFunction, blockParameter);
        }

        
        public Task<BigInteger> TotalStakedQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TotalStakedFunction, BigInteger>(null, blockParameter);
        }

        public Task<string> TransferOwnershipRequestAsync(TransferOwnershipFunction transferOwnershipFunction)
        {
             return ContractHandler.SendRequestAsync(transferOwnershipFunction);
        }

        public Task<TransactionReceipt> TransferOwnershipRequestAndWaitForReceiptAsync(TransferOwnershipFunction transferOwnershipFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(transferOwnershipFunction, cancellationToken);
        }

        public Task<string> TransferOwnershipRequestAsync(string newOwner)
        {
            var transferOwnershipFunction = new TransferOwnershipFunction();
                transferOwnershipFunction.NewOwner = newOwner;
            
             return ContractHandler.SendRequestAsync(transferOwnershipFunction);
        }

        public Task<TransactionReceipt> TransferOwnershipRequestAndWaitForReceiptAsync(string newOwner, CancellationTokenSource cancellationToken = null)
        {
            var transferOwnershipFunction = new TransferOwnershipFunction();
                transferOwnershipFunction.NewOwner = newOwner;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(transferOwnershipFunction, cancellationToken);
        }

        public Task<bool> UniqueIDUsedQueryAsync(UniqueIDUsedFunction uniqueIDUsedFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<UniqueIDUsedFunction, bool>(uniqueIDUsedFunction, blockParameter);
        }

        
        public Task<bool> UniqueIDUsedQueryAsync(string returnValue1, BlockParameter blockParameter = null)
        {
            var uniqueIDUsedFunction = new UniqueIDUsedFunction();
                uniqueIDUsedFunction.ReturnValue1 = returnValue1;
            
            return ContractHandler.QueryAsync<UniqueIDUsedFunction, bool>(uniqueIDUsedFunction, blockParameter);
        }

        public Task<string> UniqueIDmapQueryAsync(UniqueIDmapFunction uniqueIDmapFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<UniqueIDmapFunction, string>(uniqueIDmapFunction, blockParameter);
        }

        
        public Task<string> UniqueIDmapQueryAsync(string returnValue1, BlockParameter blockParameter = null)
        {
            var uniqueIDmapFunction = new UniqueIDmapFunction();
                uniqueIDmapFunction.ReturnValue1 = returnValue1;
            
            return ContractHandler.QueryAsync<UniqueIDmapFunction, string>(uniqueIDmapFunction, blockParameter);
        }

        public Task<string> WithdrawStakeRequestAsync(WithdrawStakeFunction withdrawStakeFunction)
        {
             return ContractHandler.SendRequestAsync(withdrawStakeFunction);
        }

        public Task<string> WithdrawStakeRequestAsync()
        {
             return ContractHandler.SendRequestAsync<WithdrawStakeFunction>();
        }

        public Task<TransactionReceipt> WithdrawStakeRequestAndWaitForReceiptAsync(WithdrawStakeFunction withdrawStakeFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(withdrawStakeFunction, cancellationToken);
        }

        public Task<TransactionReceipt> WithdrawStakeRequestAndWaitForReceiptAsync(CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync<WithdrawStakeFunction>(null, cancellationToken);
        }

        public override List<Type> GetAllFunctionTypes()
        {
            return new List<Type>
            {
                typeof(DistributeResultsBeginNextCycleFunction),
                typeof(TokenAddressFunction),
                typeof(ActivityThresholdFunction),
                typeof(AllowWithdrawFunction),
                typeof(ChallengerCountFunction),
                typeof(CheckResultsFunction),
                typeof(CreatePledgeFunction),
                typeof(CycleFunction),
                typeof(DisableWithdrawFunction),
                typeof(EnableWithdrawFunction),
                typeof(HasStakedFunction),
                typeof(MinuteThresholdFunction),
                typeof(OwnerFunction),
                typeof(ParticipantsFunction),
                typeof(PendingResultsFunction),
                typeof(RenounceOwnershipFunction),
                typeof(ResultThisCycleFunction),
                typeof(RewardTokensFunction),
                typeof(RewardsPoolFunction),
                typeof(SetResultFunction),
                typeof(StakeCompleteFunction),
                typeof(StakedTokensFunction),
                typeof(TotalStakedFunction),
                typeof(TransferOwnershipFunction),
                typeof(UniqueIDUsedFunction),
                typeof(UniqueIDmapFunction),
                typeof(WithdrawStakeFunction)
            };
        }

        public override List<Type> GetAllEventTypes()
        {
            return new List<Type>
            {
                typeof(OwnershipTransferredEventDTO)
            };
        }

        public override List<Type> GetAllErrorTypes()
        {
            return new List<Type>
            {
                typeof(OwnableInvalidOwnerError),
                typeof(OwnableUnauthorizedAccountError)
            };
        }
    }
}
