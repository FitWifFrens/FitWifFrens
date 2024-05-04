// SPDX-License-Identifier: MIT
pragma solidity 0.8.23;

import "@openzeppelin/contracts/token/ERC20/IERC20.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

contract TokenStaking is Ownable(msg.sender) { //pass msg.sender as initialOwner
    IERC20 public TokenAddress;
    mapping(address => uint256) public stakedTokens;
    uint256 public totalStaked;
    uint256 public activityThreshold;
    uint256 public minuteThreshold;


    constructor(address _tokenAddress, uint256 _activityThreshold, uint256 _minuteThreshold) {
        TokenAddress = IERC20(_tokenAddress);
        activityThreshold = _activityThreshold;
        minuteThreshold = _minuteThreshold;
    }
    // need to include ID information during staking for check

    // add a check for the worldId token to be used once
    function stakeToken(uint256 amount) public {
        require(TokenAddress.transferFrom(msg.sender, address(this), amount), "Transfer failed");
        stakedTokens[msg.sender] += amount;
        totalStaked += amount;
    }

    //disable withdrawing if it is not available
    function withdrawStake(uint256 amount) public {
        require(stakedTokens[msg.sender] >= amount, "Insufficient balance");
        stakedTokens[msg.sender] -= amount;
        totalStaked -= amount;
        require(TokenAddress.transfer(msg.sender, amount), "Transfer failed");
    }

    //reward resolution
    function distributeUSDC(address[] memory participants, uint256[] memory amounts) public onlyOwner {
        require(participants.length == amounts.length, "Mismatch between participants and amounts");
        for (uint256 i = 0; i < participants.length; i++) {
            require(TokenAddress.transfer(participants[i], amounts[i]), "Transfer failed");
        }
    }

    //make chainlink function call to get data api for the current event, api will need list of user IDs ??
    function checkResults() public onlyOwner {
        //get some API check...

        //success or failure... 

        //get date from chainlink?
    }
}