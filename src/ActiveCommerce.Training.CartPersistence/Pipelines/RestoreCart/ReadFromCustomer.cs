﻿using ActiveCommerce.Training.CartPersistence.Common;
using ActiveCommerce.Training.CartPersistence.Pipelines.PersistCart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ActiveCommerce.Training.CartPersistence.Pipelines.RestoreCart
{
    public class ReadFromCustomer : IRestoreCartProcessor
    {
        public void Process(RestoreCartArgs args)
        {
            if (CartPersistenceContext.CustomerRestoreStrategy == CustomerRestoreStrategy.None)
            {
                return;
            }

            var user = args.CustomerManager.CurrentUser;
            if (user == null || string.IsNullOrEmpty(user.NickName) || Sitecore.Context.Domain.IsAnonymousUser(user.NickName))
            {
                return;
            }

            if (CartPersistenceContext.CustomerRestoreStrategy == CustomerRestoreStrategy.OverwriteIfEmpty &&
                args.CartItems.Any() || args.ShoppingCart.ShoppingCartLines.Any())
            {
                return;
            }

            if (CartPersistenceContext.CustomerRestoreStrategy == CustomerRestoreStrategy.Overwrite)
            {
                args.CartItems.Clear();
                using (args.ShoppingCart.DisableEvents(false))
                {
                    args.ShoppingCart.ShoppingCartLines.Clear();
                }
            }

            var customerCoupon = user.CustomProperties[PersistToCustomer.CouponCodeKey];
            if (!string.IsNullOrEmpty(customerCoupon) && customerCoupon != PersistToCustomer.EmptyCart)
            {
                args.CouponCode = user.CustomProperties[PersistToCustomer.CouponCodeKey];
            }

            var customerCart = user.CustomProperties[PersistToCustomer.CartItemsKey];
            if (!string.IsNullOrEmpty(customerCart) && customerCart != PersistToCustomer.EmptyCart)
            {
                if (args.CartItems == null)
                {
                    args.CartItems = new Dictionary<string, uint>();
                }

                var cartItems = user.CustomProperties[PersistToCustomer.CartItemsKey];
                var products = cartItems.Split(',');
                foreach (var product in products)
                {
                    var codeQuantity = product.Split('|');
                    if (codeQuantity.Length != 2) continue;
                    uint quantity = 0;
                    uint.TryParse(codeQuantity[1], out quantity);
                    if (quantity < 1) continue;
                    var productCode = codeQuantity[0];
                    if (!args.CartItems.ContainsKey(productCode))
                    {
                        args.CartItems.Add(productCode, quantity);
                    }
                    else if (args.CartItems[productCode] < quantity)
                    {
                        args.CartItems[productCode] = quantity;
                    }
                }
            }

        }
    }
}