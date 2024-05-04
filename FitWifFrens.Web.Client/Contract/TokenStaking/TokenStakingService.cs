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

        public Task<string> DistributeUSDCRequestAsync(DistributeUSDCFunction distributeUSDCFunction)
        {
             return ContractHandler.SendRequestAsync(distributeUSDCFunction);
        }

        public Task<TransactionReceipt> DistributeUSDCRequestAndWaitForReceiptAsync(DistributeUSDCFunction distributeUSDCFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(distributeUSDCFunction, cancellationToken);
        }

        public Task<string> DistributeUSDCRequestAsync(List<string> participants, List<BigInteger> amounts)
        {
            var distributeUSDCFunction = new DistributeUSDCFunction();
                distributeUSDCFunction.Participants = participants;
                distributeUSDCFunction.Amounts = amounts;
            
             return ContractHandler.SendRequestAsync(distributeUSDCFunction);
        }

        public Task<TransactionReceipt> DistributeUSDCRequestAndWaitForReceiptAsync(List<string> participants, List<BigInteger> amounts, CancellationTokenSource cancellationToken = null)
        {
            var distributeUSDCFunction = new DistributeUSDCFunction();
                distributeUSDCFunction.Participants = participants;
                distributeUSDCFunction.Amounts = amounts;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(distributeUSDCFunction, cancellationToken);
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

        public Task<string> StakeTokenRequestAsync(StakeTokenFunction stakeTokenFunction)
        {
             return ContractHandler.SendRequestAsync(stakeTokenFunction);
        }

        public Task<TransactionReceipt> StakeTokenRequestAndWaitForReceiptAsync(StakeTokenFunction stakeTokenFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(stakeTokenFunction, cancellationToken);
        }

        public Task<string> StakeTokenRequestAsync(BigInteger amount)
        {
            var stakeTokenFunction = new StakeTokenFunction();
                stakeTokenFunction.Amount = amount;
            
             return ContractHandler.SendRequestAsync(stakeTokenFunction);
        }

        public Task<TransactionReceipt> StakeTokenRequestAndWaitForReceiptAsync(BigInteger amount, CancellationTokenSource cancellationToken = null)
        {
            var stakeTokenFunction = new StakeTokenFunction();
                stakeTokenFunction.Amount = amount;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(stakeTokenFunction, cancellationToken);
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

        public Task<string> WithdrawStakeRequestAsync(WithdrawStakeFunction withdrawStakeFunction)
        {
             return ContractHandler.SendRequestAsync(withdrawStakeFunction);
        }

        public Task<TransactionReceipt> WithdrawStakeRequestAndWaitForReceiptAsync(WithdrawStakeFunction withdrawStakeFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(withdrawStakeFunction, cancellationToken);
        }

        public Task<string> WithdrawStakeRequestAsync(BigInteger amount)
        {
            var withdrawStakeFunction = new WithdrawStakeFunction();
                withdrawStakeFunction.Amount = amount;
            
             return ContractHandler.SendRequestAsync(withdrawStakeFunction);
        }

        public Task<TransactionReceipt> WithdrawStakeRequestAndWaitForReceiptAsync(BigInteger amount, CancellationTokenSource cancellationToken = null)
        {
            var withdrawStakeFunction = new WithdrawStakeFunction();
                withdrawStakeFunction.Amount = amount;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(withdrawStakeFunction, cancellationToken);
        }

        public override List<Type> GetAllFunctionTypes()
        {
            return new List<Type>
            {
                typeof(TokenAddressFunction),
                typeof(ActivityThresholdFunction),
                typeof(CheckResultsFunction),
                typeof(DistributeUSDCFunction),
                typeof(MinuteThresholdFunction),
                typeof(OwnerFunction),
                typeof(RenounceOwnershipFunction),
                typeof(StakeTokenFunction),
                typeof(StakedTokensFunction),
                typeof(TotalStakedFunction),
                typeof(TransferOwnershipFunction),
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
