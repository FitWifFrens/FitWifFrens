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

        public Task<byte[]> DefaultAdminRoleQueryAsync(DefaultAdminRoleFunction defaultAdminRoleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DefaultAdminRoleFunction, byte[]>(defaultAdminRoleFunction, blockParameter);
        }

        
        public Task<byte[]> DefaultAdminRoleQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<DefaultAdminRoleFunction, byte[]>(null, blockParameter);
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

        public Task<byte[]> ManagerRoleQueryAsync(ManagerRoleFunction managerRoleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ManagerRoleFunction, byte[]>(managerRoleFunction, blockParameter);
        }

        
        public Task<byte[]> ManagerRoleQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ManagerRoleFunction, byte[]>(null, blockParameter);
        }

        public Task<string> TokenAddressQueryAsync(TokenAddressFunction tokenAddressFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TokenAddressFunction, string>(tokenAddressFunction, blockParameter);
        }

        
        public Task<string> TokenAddressQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TokenAddressFunction, string>(null, blockParameter);
        }

        public Task<bool> AllowWithdrawQueryAsync(AllowWithdrawFunction allowWithdrawFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<AllowWithdrawFunction, bool>(allowWithdrawFunction, blockParameter);
        }

        
        public Task<bool> AllowWithdrawQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<AllowWithdrawFunction, bool>(null, blockParameter);
        }

        public Task<string> BytesToStringQueryAsync(BytesToStringFunction bytesToStringFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<BytesToStringFunction, string>(bytesToStringFunction, blockParameter);
        }

        
        public Task<string> BytesToStringQueryAsync(byte[] data, BlockParameter blockParameter = null)
        {
            var bytesToStringFunction = new BytesToStringFunction();
                bytesToStringFunction.Data = data;
            
            return ContractHandler.QueryAsync<BytesToStringFunction, string>(bytesToStringFunction, blockParameter);
        }

        public Task<List<BigInteger>> BytesToUintArrayQueryAsync(BytesToUintArrayFunction bytesToUintArrayFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<BytesToUintArrayFunction, List<BigInteger>>(bytesToUintArrayFunction, blockParameter);
        }

        
        public Task<List<BigInteger>> BytesToUintArrayQueryAsync(byte[] data, BlockParameter blockParameter = null)
        {
            var bytesToUintArrayFunction = new BytesToUintArrayFunction();
                bytesToUintArrayFunction.Data = data;
            
            return ContractHandler.QueryAsync<BytesToUintArrayFunction, List<BigInteger>>(bytesToUintArrayFunction, blockParameter);
        }

        public Task<string> ChallengeNameQueryAsync(ChallengeNameFunction challengeNameFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ChallengeNameFunction, string>(challengeNameFunction, blockParameter);
        }

        
        public Task<string> ChallengeNameQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ChallengeNameFunction, string>(null, blockParameter);
        }

        public Task<BigInteger> ChallengerCountQueryAsync(ChallengerCountFunction challengerCountFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ChallengerCountFunction, BigInteger>(challengerCountFunction, blockParameter);
        }

        
        public Task<BigInteger> ChallengerCountQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ChallengerCountFunction, BigInteger>(null, blockParameter);
        }

        public Task<bool> CheckResultsQueryAsync(CheckResultsFunction checkResultsFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<CheckResultsFunction, bool>(checkResultsFunction, blockParameter);
        }

        
        public Task<bool> CheckResultsQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<CheckResultsFunction, bool>(null, blockParameter);
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

        public Task<List<string>> GetParticipantsQueryAsync(GetParticipantsFunction getParticipantsFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<GetParticipantsFunction, List<string>>(getParticipantsFunction, blockParameter);
        }

        
        public Task<List<string>> GetParticipantsQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<GetParticipantsFunction, List<string>>(null, blockParameter);
        }

        public Task<byte[]> GetRoleAdminQueryAsync(GetRoleAdminFunction getRoleAdminFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<GetRoleAdminFunction, byte[]>(getRoleAdminFunction, blockParameter);
        }

        
        public Task<byte[]> GetRoleAdminQueryAsync(byte[] role, BlockParameter blockParameter = null)
        {
            var getRoleAdminFunction = new GetRoleAdminFunction();
                getRoleAdminFunction.Role = role;
            
            return ContractHandler.QueryAsync<GetRoleAdminFunction, byte[]>(getRoleAdminFunction, blockParameter);
        }

        public Task<string> GrantRoleRequestAsync(GrantRoleFunction grantRoleFunction)
        {
             return ContractHandler.SendRequestAsync(grantRoleFunction);
        }

        public Task<TransactionReceipt> GrantRoleRequestAndWaitForReceiptAsync(GrantRoleFunction grantRoleFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(grantRoleFunction, cancellationToken);
        }

        public Task<string> GrantRoleRequestAsync(byte[] role, string account)
        {
            var grantRoleFunction = new GrantRoleFunction();
                grantRoleFunction.Role = role;
                grantRoleFunction.Account = account;
            
             return ContractHandler.SendRequestAsync(grantRoleFunction);
        }

        public Task<TransactionReceipt> GrantRoleRequestAndWaitForReceiptAsync(byte[] role, string account, CancellationTokenSource cancellationToken = null)
        {
            var grantRoleFunction = new GrantRoleFunction();
                grantRoleFunction.Role = role;
                grantRoleFunction.Account = account;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(grantRoleFunction, cancellationToken);
        }

        public Task<bool> HasRoleQueryAsync(HasRoleFunction hasRoleFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<HasRoleFunction, bool>(hasRoleFunction, blockParameter);
        }

        
        public Task<bool> HasRoleQueryAsync(byte[] role, string account, BlockParameter blockParameter = null)
        {
            var hasRoleFunction = new HasRoleFunction();
                hasRoleFunction.Role = role;
                hasRoleFunction.Account = account;
            
            return ContractHandler.QueryAsync<HasRoleFunction, bool>(hasRoleFunction, blockParameter);
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

        public Task<string> LastSuccessfulIndicesQueryAsync(LastSuccessfulIndicesFunction lastSuccessfulIndicesFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<LastSuccessfulIndicesFunction, string>(lastSuccessfulIndicesFunction, blockParameter);
        }

        
        public Task<string> LastSuccessfulIndicesQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<LastSuccessfulIndicesFunction, string>(null, blockParameter);
        }

        public Task<List<BigInteger>> ParseStringToArrayQueryAsync(ParseStringToArrayFunction parseStringToArrayFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<ParseStringToArrayFunction, List<BigInteger>>(parseStringToArrayFunction, blockParameter);
        }

        
        public Task<List<BigInteger>> ParseStringToArrayQueryAsync(string str, BlockParameter blockParameter = null)
        {
            var parseStringToArrayFunction = new ParseStringToArrayFunction();
                parseStringToArrayFunction.Str = str;
            
            return ContractHandler.QueryAsync<ParseStringToArrayFunction, List<BigInteger>>(parseStringToArrayFunction, blockParameter);
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

        public Task<string> RenounceRoleRequestAsync(RenounceRoleFunction renounceRoleFunction)
        {
             return ContractHandler.SendRequestAsync(renounceRoleFunction);
        }

        public Task<TransactionReceipt> RenounceRoleRequestAndWaitForReceiptAsync(RenounceRoleFunction renounceRoleFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(renounceRoleFunction, cancellationToken);
        }

        public Task<string> RenounceRoleRequestAsync(byte[] role, string callerConfirmation)
        {
            var renounceRoleFunction = new RenounceRoleFunction();
                renounceRoleFunction.Role = role;
                renounceRoleFunction.CallerConfirmation = callerConfirmation;
            
             return ContractHandler.SendRequestAsync(renounceRoleFunction);
        }

        public Task<TransactionReceipt> RenounceRoleRequestAndWaitForReceiptAsync(byte[] role, string callerConfirmation, CancellationTokenSource cancellationToken = null)
        {
            var renounceRoleFunction = new RenounceRoleFunction();
                renounceRoleFunction.Role = role;
                renounceRoleFunction.CallerConfirmation = callerConfirmation;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(renounceRoleFunction, cancellationToken);
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

        public Task<string> RevokeRoleRequestAsync(RevokeRoleFunction revokeRoleFunction)
        {
             return ContractHandler.SendRequestAsync(revokeRoleFunction);
        }

        public Task<TransactionReceipt> RevokeRoleRequestAndWaitForReceiptAsync(RevokeRoleFunction revokeRoleFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(revokeRoleFunction, cancellationToken);
        }

        public Task<string> RevokeRoleRequestAsync(byte[] role, string account)
        {
            var revokeRoleFunction = new RevokeRoleFunction();
                revokeRoleFunction.Role = role;
                revokeRoleFunction.Account = account;
            
             return ContractHandler.SendRequestAsync(revokeRoleFunction);
        }

        public Task<TransactionReceipt> RevokeRoleRequestAndWaitForReceiptAsync(byte[] role, string account, CancellationTokenSource cancellationToken = null)
        {
            var revokeRoleFunction = new RevokeRoleFunction();
                revokeRoleFunction.Role = role;
                revokeRoleFunction.Account = account;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(revokeRoleFunction, cancellationToken);
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

        public Task<string> SetAllResultRequestAsync(SetAllResultFunction setAllResultFunction)
        {
             return ContractHandler.SendRequestAsync(setAllResultFunction);
        }

        public Task<TransactionReceipt> SetAllResultRequestAndWaitForReceiptAsync(SetAllResultFunction setAllResultFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setAllResultFunction, cancellationToken);
        }

        public Task<string> SetAllResultRequestAsync(List<BigInteger> successfulIndices)
        {
            var setAllResultFunction = new SetAllResultFunction();
                setAllResultFunction.SuccessfulIndices = successfulIndices;
            
             return ContractHandler.SendRequestAsync(setAllResultFunction);
        }

        public Task<TransactionReceipt> SetAllResultRequestAndWaitForReceiptAsync(List<BigInteger> successfulIndices, CancellationTokenSource cancellationToken = null)
        {
            var setAllResultFunction = new SetAllResultFunction();
                setAllResultFunction.SuccessfulIndices = successfulIndices;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setAllResultFunction, cancellationToken);
        }

        public Task<string> SetLastSuccessfulIndicesRequestAsync(SetLastSuccessfulIndicesFunction setLastSuccessfulIndicesFunction)
        {
             return ContractHandler.SendRequestAsync(setLastSuccessfulIndicesFunction);
        }

        public Task<TransactionReceipt> SetLastSuccessfulIndicesRequestAndWaitForReceiptAsync(SetLastSuccessfulIndicesFunction setLastSuccessfulIndicesFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setLastSuccessfulIndicesFunction, cancellationToken);
        }

        public Task<string> SetLastSuccessfulIndicesRequestAsync(string successfulIndices)
        {
            var setLastSuccessfulIndicesFunction = new SetLastSuccessfulIndicesFunction();
                setLastSuccessfulIndicesFunction.SuccessfulIndices = successfulIndices;
            
             return ContractHandler.SendRequestAsync(setLastSuccessfulIndicesFunction);
        }

        public Task<TransactionReceipt> SetLastSuccessfulIndicesRequestAndWaitForReceiptAsync(string successfulIndices, CancellationTokenSource cancellationToken = null)
        {
            var setLastSuccessfulIndicesFunction = new SetLastSuccessfulIndicesFunction();
                setLastSuccessfulIndicesFunction.SuccessfulIndices = successfulIndices;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(setLastSuccessfulIndicesFunction, cancellationToken);
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

        public Task<bool> SupportsInterfaceQueryAsync(SupportsInterfaceFunction supportsInterfaceFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<SupportsInterfaceFunction, bool>(supportsInterfaceFunction, blockParameter);
        }

        
        public Task<bool> SupportsInterfaceQueryAsync(byte[] interfaceId, BlockParameter blockParameter = null)
        {
            var supportsInterfaceFunction = new SupportsInterfaceFunction();
                supportsInterfaceFunction.InterfaceId = interfaceId;
            
            return ContractHandler.QueryAsync<SupportsInterfaceFunction, bool>(supportsInterfaceFunction, blockParameter);
        }

        public Task<List<BigInteger>> TestParseStringToArrayQueryAsync(TestParseStringToArrayFunction testParseStringToArrayFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TestParseStringToArrayFunction, List<BigInteger>>(testParseStringToArrayFunction, blockParameter);
        }

        
        public Task<List<BigInteger>> TestParseStringToArrayQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TestParseStringToArrayFunction, List<BigInteger>>(null, blockParameter);
        }

        public Task<BigInteger> TotalStakedQueryAsync(TotalStakedFunction totalStakedFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TotalStakedFunction, BigInteger>(totalStakedFunction, blockParameter);
        }

        
        public Task<BigInteger> TotalStakedQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<TotalStakedFunction, BigInteger>(null, blockParameter);
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

        public Task<string> WithdrawAllRequestAsync(WithdrawAllFunction withdrawAllFunction)
        {
             return ContractHandler.SendRequestAsync(withdrawAllFunction);
        }

        public Task<string> WithdrawAllRequestAsync()
        {
             return ContractHandler.SendRequestAsync<WithdrawAllFunction>();
        }

        public Task<TransactionReceipt> WithdrawAllRequestAndWaitForReceiptAsync(WithdrawAllFunction withdrawAllFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(withdrawAllFunction, cancellationToken);
        }

        public Task<TransactionReceipt> WithdrawAllRequestAndWaitForReceiptAsync(CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync<WithdrawAllFunction>(null, cancellationToken);
        }

        public Task<string> WithdrawRewardsRequestAsync(WithdrawRewardsFunction withdrawRewardsFunction)
        {
             return ContractHandler.SendRequestAsync(withdrawRewardsFunction);
        }

        public Task<string> WithdrawRewardsRequestAsync()
        {
             return ContractHandler.SendRequestAsync<WithdrawRewardsFunction>();
        }

        public Task<TransactionReceipt> WithdrawRewardsRequestAndWaitForReceiptAsync(WithdrawRewardsFunction withdrawRewardsFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(withdrawRewardsFunction, cancellationToken);
        }

        public Task<TransactionReceipt> WithdrawRewardsRequestAndWaitForReceiptAsync(CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync<WithdrawRewardsFunction>(null, cancellationToken);
        }

        public override List<Type> GetAllFunctionTypes()
        {
            return new List<Type>
            {
                typeof(DefaultAdminRoleFunction),
                typeof(DistributeResultsBeginNextCycleFunction),
                typeof(ManagerRoleFunction),
                typeof(TokenAddressFunction),
                typeof(AllowWithdrawFunction),
                typeof(BytesToStringFunction),
                typeof(BytesToUintArrayFunction),
                typeof(ChallengeNameFunction),
                typeof(ChallengerCountFunction),
                typeof(CheckResultsFunction),
                typeof(CreatePledgeFunction),
                typeof(CycleFunction),
                typeof(DisableWithdrawFunction),
                typeof(EnableWithdrawFunction),
                typeof(GetParticipantsFunction),
                typeof(GetRoleAdminFunction),
                typeof(GrantRoleFunction),
                typeof(HasRoleFunction),
                typeof(HasStakedFunction),
                typeof(LastSuccessfulIndicesFunction),
                typeof(ParseStringToArrayFunction),
                typeof(ParticipantsFunction),
                typeof(PendingResultsFunction),
                typeof(RenounceRoleFunction),
                typeof(ResultThisCycleFunction),
                typeof(RevokeRoleFunction),
                typeof(RewardTokensFunction),
                typeof(RewardsPoolFunction),
                typeof(SetAllResultFunction),
                typeof(SetLastSuccessfulIndicesFunction),
                typeof(SetResultFunction),
                typeof(StakeCompleteFunction),
                typeof(StakedTokensFunction),
                typeof(SupportsInterfaceFunction),
                typeof(TestParseStringToArrayFunction),
                typeof(TotalStakedFunction),
                typeof(UniqueIDUsedFunction),
                typeof(UniqueIDmapFunction),
                typeof(WithdrawAllFunction),
                typeof(WithdrawRewardsFunction)
            };
        }

        public override List<Type> GetAllEventTypes()
        {
            return new List<Type>
            {
                typeof(FitResultsEventDTO),
                typeof(RoleAdminChangedEventDTO),
                typeof(RoleGrantedEventDTO),
                typeof(RoleRevokedEventDTO)
            };
        }

        public override List<Type> GetAllErrorTypes()
        {
            return new List<Type>
            {
                typeof(AccessControlBadConfirmationError),
                typeof(AccessControlUnauthorizedAccountError)
            };
        }
    }
}
