// SPDX-License-Identifier: MIT
pragma solidity 0.8.23;

import "@openzeppelin/contracts/token/ERC20/IERC20.sol";
import "@openzeppelin/contracts/access/Ownable.sol";


contract TokenStakingFitChallenge is Ownable(msg.sender) { //pass msg.sender as initialOwner
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
    uint256 public activityThreshold;
    uint256 public minuteThreshold;
    uint256 private rewardPerParticipant;
    bool public pendingResults;
    bool public allowWithdraw = true;
    uint public cycle = 1;


    constructor(address _tokenAddress, uint256 _activityThreshold, uint256 _minuteThreshold) {
        TokenAddress = IERC20(_tokenAddress);
        activityThreshold = _activityThreshold;
        minuteThreshold = _minuteThreshold;
    }
    // need to include ID information during staking for check
    function createPledge(uint256 amount, string memory uniqueID) public {
        require(TokenAddress.transferFrom(msg.sender, address(this), amount), "Transfer failed");        
        if (!hasStaked[msg.sender]) {
            participants.push(msg.sender);
            hasStaked[msg.sender] = true;
        }
        resultThisCycle[msg.sender]=false;
        uniqueIDmap[msg.sender] = uniqueID;
        stakedTokens[msg.sender] += amount;
        totalStaked += amount;
        challengerCount += 1;

    }

    // add a check for the worldId token to be used once
    function stakeToken(uint256 amount) private {
        require(TokenAddress.transferFrom(msg.sender, address(this), amount), "Transfer failed");
        if (!hasStaked[msg.sender]) {
            participants.push(msg.sender);
            hasStaked[msg.sender] = true;
        }
        stakedTokens[msg.sender] += amount;
        totalStaked += amount;
    }

    //disable withdrawing if it is not available
    function withdrawStake() public {
        require(allowWithdraw, "Withdraw not allowed");
        uint256 amount = stakedTokens[msg.sender];
        require(amount>0, "Insufficient balance");
        stakedTokens[msg.sender] -= amount;
        totalStaked -= amount;
        require(TokenAddress.transfer(msg.sender, amount), "Transfer failed");
    }

    //disable withdrawing on cotnract
    function disableWithdraw() public onlyOwner {
        allowWithdraw = false;
    }

     //disable withdrawing on cotnract
    function enableWithdraw() public onlyOwner {
        allowWithdraw = true;
    }
    
    

    //reward resolution
    function DistributeResultsBeginNextCycle() public onlyOwner {
        //require(participants.length == amounts.length, "Mismatch between participants and amounts");
        //uint256 rewardTokens = 0;
        uint256 successfulParticipants = 0;
        for (uint256 i = 0; i < participants.length; i++) {
            if(!resultThisCycle[participants[i]]){
                //failed Test this cycle
                uint256 amount = stakedTokens[participants[i]];
                stakedTokens[participants[i]] = 0;
                totalStaked -= amount;
                rewardsPool += amount;
            } else {
                //passed Test this cycle
                successfulParticipants += 1;

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


    }

    //make chainlink function call to get data api for the current event, api will need list of user IDs ??
    function checkResults() public onlyOwner {
        //get some API check...
        //_ api results in , ID, activity, time
        

        //success or failure... 

        //get date from chainlink?
    }

    function setResult(address _userAddress,bool resultstate) public onlyOwner {
        resultThisCycle[_userAddress]=resultstate;
    }
}