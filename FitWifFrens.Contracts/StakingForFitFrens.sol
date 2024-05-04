// SPDX-License-Identifier: MIT
pragma solidity 0.8.23;

import "@openzeppelin/contracts/token/ERC20/IERC20.sol";
import "@openzeppelin/contracts/access/AccessControl.sol";


contract StakingForFitFrens is AccessControl { 
    
    bytes32 public constant MANAGER_ROLE = keccak256("MANAGER_ROLE");
    IERC20 public TokenAddress;
    mapping(address => uint256) public stakedTokens;
    mapping(address => uint256) public rewardTokens;
    mapping(address => bool) public stakeComplete;
    mapping(address => bool) public hasStaked;
    mapping(address => string) public uniqueIDmap;
    mapping(string => bool) public uniqueIDUsed;
    mapping(address=>bool) public resultThisCycle;
    address[] public participants;

    uint256 public totalStaked;
    uint256 public rewardsPool;
    uint256 public challengerCount;
    uint256 private activityThreshold;
    uint256 private minuteThreshold;
    uint256 private rewardPerParticipant;
    bool public pendingResults;
    bool public allowWithdraw = true;
    uint public cycle = 1;
    string public challengeName;
    string public lastSuccessfulIndices = "none";

    // Event to log responses
    event FitResults(
        string successes
    );


    constructor(address _tokenAddress, uint256 _activityThreshold, uint256 _minuteThreshold, string memory _challengeName) {
        _grantRole(DEFAULT_ADMIN_ROLE, msg.sender);
        _grantRole(MANAGER_ROLE, msg.sender);
        TokenAddress = IERC20(_tokenAddress);
        activityThreshold = _activityThreshold;
        minuteThreshold = _minuteThreshold;
        challengeName = _challengeName;
    }
    // include uniqueID for check
    function createPledge(uint256 amount, string memory uniqueID) public {
        require(TokenAddress.transferFrom(msg.sender, address(this), amount), "Transfer failed");        
        if (!hasStaked[msg.sender]) {
            participants.push(msg.sender);
            hasStaked[msg.sender] = true;
        }
        resultThisCycle[msg.sender]=false;
        uniqueIDmap[msg.sender] = uniqueID;
        stakeComplete[msg.sender]=false;
        stakedTokens[msg.sender] += amount;
        totalStaked += amount;
        challengerCount += 1;

    }  

    //disable withdrawing if it is not available
    function withdrawAll() public {
        require(stakeComplete[msg.sender],"staking period not over");
        require(allowWithdraw, "Withdraw not allowed");
        uint256 amount = stakedTokens[msg.sender];
        uint256 rewardAmount = rewardTokens[msg.sender];
        require(amount>0, "Insufficient balance");
        stakedTokens[msg.sender] -= amount;
        rewardTokens[msg.sender] -= rewardAmount;
        totalStaked -= amount;

        require(TokenAddress.transfer(msg.sender, amount + rewardAmount), "Transfer failed");
    }

    function withdrawRewards() public {
        require(allowWithdraw, "Withdraw not allowed");
        uint256 rewardAmount = rewardTokens[msg.sender];
        require(rewardAmount>0, "Insufficient balance");
        rewardTokens[msg.sender] -= rewardAmount;

        require(TokenAddress.transfer(msg.sender, rewardAmount), "Transfer failed");

    }

    //disable withdrawing on contract
    function disableWithdraw() public onlyRole(MANAGER_ROLE) {
        allowWithdraw = false;
    }

     //disable withdrawing on contract
    function enableWithdraw() public onlyRole(MANAGER_ROLE) {
        allowWithdraw = true;
    }
    
    

    //reward resolution
    function DistributeResultsBeginNextCycle() public onlyRole(MANAGER_ROLE) {
        //require(participants.length == amounts.length, "Mismatch between participants and amounts");
        //uint256 rewardTokens = 0;
        require(checkResults(),"No results, unable to distribute");
        setAllResult(parseStringToArray(lastSuccessfulIndices));
        uint256 successfulParticipants = 0;
        for (uint256 i = 0; i < participants.length; i++) {
            if(!resultThisCycle[participants[i]]){
                //failed Test this cycle
                uint256 amount = stakedTokens[participants[i]];
                stakedTokens[participants[i]] = 0;
                totalStaked -= amount;
                rewardsPool += amount;
                challengerCount -=1;
            } else {
                //passed Test this cycle
                successfulParticipants += 1;    
                stakeComplete[participants[i]]=true;            

            }
        }

        if(rewardsPool>0 && successfulParticipants >0 ){
            rewardPerParticipant = rewardsPool/successfulParticipants;      

        }

        if(rewardPerParticipant > 0){
            for (uint256 i = 0; i < participants.length; i++) {
            if(resultThisCycle[participants[i]]){
                //passed Test this cycle
                rewardTokens[participants[i]] += rewardPerParticipant;
                rewardsPool -= rewardPerParticipant;
                resultThisCycle[participants[i]]=false;
            } 
            }
        }

        cycle += 1;
        lastSuccessfulIndices = "none";


    }

    //make chainlink function call to get data api for the current event, api will need list of user IDs ??
    function checkResults() public onlyRole(MANAGER_ROLE) view returns (bool result) {
        //get some API check...
        //_ api results in , ID, activity, time
        if(keccak256(abi.encodePacked(lastSuccessfulIndices)) == keccak256(abi.encodePacked("none"))){
            return false;
        }
            return true;
    }

    function setResult(address _userAddress,bool resultstate) public onlyRole(MANAGER_ROLE) {
        resultThisCycle[_userAddress]=resultstate;
        
    }

    function setLastSuccessfulIndices(string memory _successfulIndices) public onlyRole(MANAGER_ROLE) {
        lastSuccessfulIndices = _successfulIndices;
        // Emit an event to log the response
        emit FitResults(_successfulIndices);
    }

    function setAllResult(uint[] memory _successfulIndices) public onlyRole(MANAGER_ROLE) {
        // Set resultThisCycle to true for successful participants
        for (uint256 i = 0; i < _successfulIndices.length; i++) {
            if(stakedTokens[participants[_successfulIndices[i]]]>0 && participants.length > _successfulIndices[i]){
                resultThisCycle[participants[_successfulIndices[i]]] = true;
            }
        }
    }

    function bytesToUintArray(bytes memory data) public pure returns (uint256[] memory) {
    string memory str = bytesToString(data);
    return parseStringToArray(str);
    }

    function bytesToString(bytes memory data) public pure returns (string memory) {
    return string(data);
    }

    function parseStringToArray(string memory str) public pure returns (uint256[] memory) {
        bytes memory b = bytes(str);
        uint256[] memory result = new uint256[](b.length);

        uint256 counter = 0;
        for (uint256 i = 0; i < b.length; i++) {
            if (uint8(b[i]) >= 48 && uint8(b[i]) <= 57) {
                uint256 number = uint256(uint8(b[i])) - 48; // convert to uint
                result[counter] = number;
                counter++;
            }
        }

        // resize the array
        uint256[] memory finalResult = new uint256[](counter);
        for (uint256 i = 0; i < counter; i++) {
            finalResult[i] = result[i];
        }

    return finalResult;
}

    function getParticipants() public view returns (address[] memory) {
        return participants;
    }
}