#!/bin/bash

echo "Checking SES verified email addresses in us-east-2..."
aws ses list-verified-email-addresses --region us-east-2

echo -e "\nChecking SES sending quota..."
aws ses get-send-quota --region us-east-2

echo -e "\nChecking SES sending statistics..."
aws ses get-send-statistics --region us-east-2

echo -e "\nChecking if email is in suppression list..."
aws sesv2 get-suppressed-destination --email-address sbennington@val-co.com --region us-east-2 2>/dev/null || echo "Email not in suppression list"