// SPDX-License-Identifier: MIT
pragma solidity 0.8.19;


//create Interface for TokenSTakingFitChallenge
interface IStakingForFitFrens {
    function createPledge(uint256 amount, string memory uniqueID) external;
    function stakeToken(uint256 amount) external;
    function withdrawAll() external;
    function withdrawRewards() external;
    function disableWithdraw() external;
    function enableWithdraw() external;
    function DistributeResultsBeginNextCycle() external;
    function checkResults() external;
    function setResult(address _userAddress,bool resultstate) external;
    function setAllResult(uint[] memory _successfulIndices) external;
    function bytesToUintArray(bytes memory data) external pure returns (uint256[] memory);
    function bytesToString(bytes memory data) external pure returns (string memory);
    function parseStringToArray(string memory str) external pure returns (uint256[] memory);
    function getParticipants() external view returns (address[] memory);
    function setLastSuccessfulIndices(string memory _successfulIndices) external;
    }

